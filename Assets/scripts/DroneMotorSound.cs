using UnityEngine;

/// <summary>
/// The raw FPV motor sound (docs/plans/motor-sound.md), synthesised in code like every effect here
/// (docs/decisions.md #16) - no audio file. Four motors, each an anti-aliased sawtooth at its
/// blade-pass frequency, plus "chop": noise gated by every blade pass, which is the rasp of a real
/// quad. A saturation stage adds grit. Pitch follows the THROTTLE; stick inputs mostly add load
/// (rasp, loudness) and only nudge the motors apart. Pure tones sliding apart - the first version -
/// sounded like a theremin ("aliens abducting me", Viktor, 06.10.2026).
///
/// Update (main thread) turns the pilot's inputs into a target speed per motor through a quad-X mix.
/// OnAudioFilterRead (AUDIO thread) chases those targets per sample, so the pitch glides without
/// zipper steps. The audio thread must not touch the Unity API: everything it reads is a plain field
/// written in Update.
///
/// The AudioSource plays a silent looping carrier clip only to keep the filter running; every sample
/// is overwritten here. 2D (spatialBlend 0): the camera is on the drone, so the sound is "us".
/// </summary>
[RequireComponent(typeof(DroneControls))]
[RequireComponent(typeof(AudioSource))]
public class DroneMotorSound : MonoBehaviour
{
    [Range(0f, 1f)]
    public float volume = 0.35f;

    [Header("Motors")]
    [Tooltip("Blade-pass frequency at idle and at full speed, Hz.")]
    public float minFrequency = 120f;
    public float maxFrequency = 780f;

    [Tooltip("Speed the motors spin at with zero throttle while flying (airmode idle). Off when stunned or crashed.")]
    [Range(0f, 0.5f)]
    public float idle = 0.08f;

    [Tooltip("How much full stick on one axis speeds up / slows down individual motors. Keep it small: large values make the four pitches slide apart (the \"alien\" sound).")]
    [Range(0f, 1f)]
    public float mixAmount = 0.12f;

    [Tooltip("How much stick deflection adds rasp and loudness - the motors working harder.")]
    [Range(0f, 1f)]
    public float stickLoad = 0.6f;

    [Tooltip("Seconds for a motor to follow a command up / down (first-order lag).")]
    public float spinUpTime = 0.04f;
    public float spinDownTime = 0.09f;

    [Tooltip("Fixed per-motor pitch offset. Tiny - larger values beat into a regular wobble (vibrato).")]
    public float detune = 0.004f;

    [Tooltip("Random pitch drift per motor, as a fraction. Breaks the beating up so it is never a steady wobble.")]
    public float jitter = 0.006f;

    [Header("Texture")]
    [Range(0f, 1f)]
    [Tooltip("Sawtooth buzz of the motors.")]
    public float buzz = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Noise chopped by each blade pass - the raw, raspy part.")]
    public float rasp = 0.55f;

    [Tooltip("Saturation. Higher = grittier, more compressed.")]
    [Range(1f, 6f)]
    public float drive = 2.5f;

    [Header("Noise")]
    [Range(0f, 1f)]
    [Tooltip("Prop wash hiss, rising with motor speed.")]
    public float propNoise = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Air rush, rising with flight speed.")]
    public float windNoise = 0.4f;

    [Tooltip("Flight speed (m/s) at which the wind noise is full.")]
    public float windFullSpeed = 40f;

    [Tooltip("Seconds to fade out when the game pauses (between waves, sandbox console) and back in.")]
    public float pauseFade = 0.15f;

    private DroneControls drone;
    private Rigidbody rb;
    private AudioSource source;
    private AudioClip carrier;

    // ---- shared with the audio thread (written in Update, read in OnAudioFilterRead) ----------
    private readonly float[] target = new float[4];
    private float windTarget;
    private float loadTarget;
    private float gainTarget;
    private int sampleRate;

    // ---- audio thread only --------------------------------------------------------------------
    private readonly float[] rpm = new float[4];
    private readonly double[] phase = new double[4];
    private readonly float[] detuneFactor = new float[4];
    private readonly float[] jit = new float[4];
    private readonly float[] jitTarget = new float[4];
    private float load;
    private float wind;
    private float gain;
    private float propLow, windLow;
    private uint noiseState = 0x9E3779B9u;

    void Awake()
    {
        drone = GetComponent<DroneControls>();
        rb = GetComponent<Rigidbody>();
        source = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;

        // Fixed spread around 1 - each motor its own pitch, the same every run.
        float[] spread = { 0f, 0.71f, -0.43f, 1.24f };
        for (int i = 0; i < 4; i++) detuneFactor[i] = 1f + spread[i] * detune;

        // A second of silence, looped. Runtime-created assets are not garbage collected - OnDestroy
        // frees it.
        carrier = AudioClip.Create("MotorCarrier", sampleRate, 1, sampleRate, false);
        source.clip = carrier;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
    }

    void Start()
    {
        source.Play();
    }

    void OnDestroy()
    {
        if (carrier != null) Destroy(carrier);
    }

    void Update()
    {
        bool motorsOn = !drone.HasCrashed && !drone.IsStunned;
        float baseline = motorsOn ? idle : 0f;

        float t = drone.ThrottleInput;
        float p = drone.PitchInput * mixAmount;
        float r = drone.RollInput * mixAmount;
        float y = drone.YawInput * mixAmount;

        // Quad X: front-right, front-left, rear-right, rear-left. Which sign goes where only decides
        // which motor sings higher; what matters is that a stick input splits them.
        target[0] = Motor(baseline, t + p - r + y);
        target[1] = Motor(baseline, t + p + r - y);
        target[2] = Motor(baseline, t - p - r - y);
        target[3] = Motor(baseline, t - p + r + y);

        loadTarget = motorsOn
            ? Mathf.Clamp01(Mathf.Abs(drone.PitchInput) + Mathf.Abs(drone.RollInput) + Mathf.Abs(drone.YawInput))
            : 0f;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        windTarget = windFullSpeed > 0f ? Mathf.Clamp01(speed / windFullSpeed) : 0f;

        // timeScale 0 freezes FixedUpdate, so the inputs would hold and the whine would drone on
        // under the card screen. Fade out instead.
        gainTarget = Time.timeScale > 0f ? volume : 0f;
    }

    float Motor(float baseline, float command)
    {
        if (baseline <= 0f) return 0f;   // stunned or crashed: the motors spin down to silence
        return baseline + (1f - baseline) * Mathf.Clamp01(command);
    }

    // AUDIO THREAD.
    void OnAudioFilterRead(float[] data, int channels)
    {
        int sr = sampleRate;
        if (sr <= 0) return;

        float dt = 1f / sr;
        float kUp = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, spinUpTime));
        float kDown = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, spinDownTime));
        float kGain = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, pauseFade));
        float kWind = 1f - Mathf.Exp(-dt / 0.25f);

        // Noise colour per block from the average command: brighter hiss as the props speed up.
        float avgTarget = (target[0] + target[1] + target[2] + target[3]) * 0.25f;
        float propCut = 1f - Mathf.Exp(-2f * Mathf.PI * (600f + 3500f * avgTarget) * dt);
        float windCut = 1f - Mathf.Exp(-2f * Mathf.PI * 350f * dt);

        float span = maxFrequency - minFrequency;
        float kJit = 1f - Mathf.Exp(-dt / 0.06f);
        float kLoad = 1f - Mathf.Exp(-dt / 0.05f);

        // A new random drift target per motor each callback (~20 ms): slow, irregular wander.
        for (int m = 0; m < 4; m++) jitTarget[m] = NextNoise();

        for (int i = 0; i < data.Length; i += channels)
        {
            float tone = 0f;
            float chop = 0f;
            float avg = 0f;
            for (int m = 0; m < 4; m++)
            {
                float goal = target[m];
                float speed = rpm[m];
                speed += (goal - speed) * (goal > speed ? kUp : kDown);
                rpm[m] = speed;
                avg += speed;

                jit[m] += (jitTarget[m] - jit[m]) * kJit;
                double f = (minFrequency + span * speed) * detuneFactor[m] * (1f + jit[m] * jitter);
                double inc = f * dt;
                double ph = phase[m] + inc;
                if (ph >= 1.0) ph -= 1.0;
                phase[m] = ph;

                // Band-limited sawtooth: rich harmonics without aliasing whistles at high rpm.
                float saw = (float)(2.0 * ph - 1.0 - PolyBlep(ph, inc));
                tone += saw * speed;

                // A short pulse at the start of every blade pass; it gates the noise below.
                float pulse = 1f - (float)ph * 4f;
                if (pulse > 0f) chop += pulse * pulse * speed;
            }
            avg *= 0.25f;

            load += (loadTarget - load) * kLoad;
            wind += (windTarget - wind) * kWind;

            float white = NextNoise();
            propLow += (white - propLow) * propCut;
            windLow += (white - windLow) * windCut;

            float working = 1f + load * stickLoad;
            float body = tone * 0.11f * buzz
                       + white * chop * 0.22f * rasp * working
                       + propLow * propNoise * avg * 0.6f * working;
            float air = windLow * windNoise * wind * 1.6f;

            gain += (gainTarget - gain) * kGain;

            // Saturate the motors for grit, then a soft clip on the whole mix so it never cracks.
            float d = body * drive;
            float x = (d / (1f + Mathf.Abs(d)) + air) * gain;
            float sample = x / (1f + Mathf.Abs(x));

            for (int c = 0; c < channels; c++) data[i + c] = sample;
        }
    }

    // PolyBLEP correction for a sawtooth at phase t (0..1) advancing dt per sample.
    static double PolyBlep(double t, double dt)
    {
        if (t < dt)
        {
            t /= dt;
            return t + t - t * t - 1.0;
        }
        if (t > 1.0 - dt)
        {
            t = (t - 1.0) / dt;
            return t * t + t + t + 1.0;
        }
        return 0.0;
    }

    // xorshift32 - System.Random and UnityEngine.Random are not for the audio thread.
    float NextNoise()
    {
        uint s = noiseState;
        s ^= s << 13;
        s ^= s >> 17;
        s ^= s << 5;
        noiseState = s;
        return (s / (float)uint.MaxValue) * 2f - 1f;
    }
}
