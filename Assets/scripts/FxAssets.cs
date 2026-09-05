using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The one place that owns the runtime-generated FX textures and materials. Everything here is built
/// lazily on first use and then shared: nothing in this project commits a .png or a .mat for effects,
/// for the same reason DroneHUD generates its vignette sprite in code (see docs/decisions.md #12) -
/// no assets, no .meta files, one reviewable C# file.
///
/// Static, not per-instance, because ImpactExplosion is created per hit. A Material and a Texture2D
/// allocated per rocket would be a leak: runtime-created Objects are not garbage collected, so at
/// Shoting.fireRate 0.5 that would be two dead native objects every half second.
/// </summary>
public static class FxAssets
{
    // URP's own particle shader. The *builtin* default particle material - the one a code-created
    // ParticleSystemRenderer arrives with - renders MAGENTA under URP, so every renderer built here
    // must be handed a material explicitly. See docs/reference/unity-gotchas.md.
    const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

    static Material additiveDot;
    static Material additiveBand;
    static Texture2D softDot;
    static Texture2D softBand;

    // "== null" rather than "is null" throughout: these survive SceneManager.LoadScene because of
    // HideAndDontSave, but a domain reload wipes the static while Unity's overloaded == still
    // reports a destroyed object correctly. "is null" would skip the rebuild and hand back a corpse.

    /// <summary>Additive material with a soft radial dot. For particle billboards.</summary>
    public static Material AdditiveDot
    {
        get
        {
            if (additiveDot == null) additiveDot = BuildAdditive(SoftDot, "FX_AdditiveDot");
            return additiveDot;
        }
    }

    /// <summary>Additive material with a soft cross-section band. For LineRenderer arcs.</summary>
    public static Material AdditiveBand
    {
        get
        {
            if (additiveBand == null) additiveBand = BuildAdditive(SoftBand, "FX_AdditiveBand");
            return additiveBand;
        }
    }

    public static Texture2D SoftDot
    {
        get
        {
            if (softDot == null) softDot = BuildSoftDot(64);
            return softDot;
        }
    }

    public static Texture2D SoftBand
    {
        get
        {
            if (softBand == null) softBand = BuildSoftBand(32);
            return softBand;
        }
    }

    static Material BuildAdditive(Texture2D map, string name)
    {
        Shader shader = Shader.Find(ParticleShader);
        if (shader == null)
        {
            // Shader.Find only sees shaders the build actually references, and nothing in this
            // project does. Always fine in the editor, null in a player build unless the shader sits
            // in Project Settings > Graphics > Always Included Shaders. See CLAUDE.md.
            Debug.LogWarning("FxAssets: shader '" + ParticleShader + "' not found. Add it to "
                             + "Project Settings > Graphics > Always Included Shaders. "
                             + "Falling back to Sprites/Default - the effect will not be additive.");
            shader = Shader.Find("Sprites/Default");
        }

        Material m = new Material(shader);
        m.name = name;
        m.hideFlags = HideFlags.HideAndDontSave; // never serialised, never unloaded under us

        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", map);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", map); // Sprites/Default fallback
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);

        // URP wires its blend state from an *editor-only* ShaderGUI (BaseShaderGUI.SetupMaterialBlendMode),
        // which does not exist at runtime. This is that method's BlendMode.Additive branch by hand.
        // ParticlesUnlit.shader declares Blend[_SrcBlend][_DstBlend] ZWrite[_ZWrite], so these floats
        // - not keywords - are what actually makes it additive.
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);  // SurfaceType.Transparent
            m.SetFloat("_Blend", 2f);    // BlendMode.Additive (enum order: Alpha, Premultiply, Additive, Multiply)
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);

            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");

            // Belt and braces. These three are the only paths in ParticlesUnlit that can zero the
            // output *silently*: `_CameraFadeParams` and `_SoftParticleFadeParams` default to
            // (0,0,0,0), which makes CameraFade() return 0 and multiply alpha away at every
            // distance. URP only fills those vectors from its editor GUI, so a runtime material that
            // ever picked up the keyword would render nothing, with no error to explain it.
            m.DisableKeyword("_FADING_ON");
            m.DisableKeyword("_SOFTPARTICLES_ON");
            m.DisableKeyword("_DISTORTION_ON");
            // _ColorMode stays 0 (Multiply), which is the one colour mode needing no keyword - so the
            // particle's vertex colour multiplies straight through. Any other mode would need
            // _COLOROVERLAY_ON / _COLORCOLOR_ON / _COLORADDSUBDIFF_ON set to match.

            m.SetOverrideTag("RenderType", "Transparent");
            m.SetShaderPassEnabled("DepthOnly", false);
        }

        m.renderQueue = (int)RenderQueue.Transparent; // 3000

        // One line per material, once per session. Turns "is the effect invisible because Shader.Find
        // silently fell back, or because of something else?" into a two-second console check - which
        // is worth having, since every failure mode in this shader is silent rather than loud.
        Debug.Log("FxAssets: built " + name + " on shader '" + m.shader.name + "', keywords ["
                  + string.Join(", ", m.shaderKeywords) + "], renderQueue " + m.renderQueue);

        return m;
    }

    /// <summary>
    /// Tints a renderer with an HDR colour without allocating a Material per effect.
    ///
    /// This exists because HDR CANNOT go through the particle system. `main.startColor` and
    /// `LineRenderer.startColor` are written into the vertex stream as Color32, so anything above 1
    /// is clamped on its way to the GPU and can never cross Bloom's threshold - the effect renders
    /// as a dull smudge instead of a glow. `_BaseColor` is a real float4 uniform and survives.
    ///
    /// A MaterialPropertyBlock rather than a per-effect Material: no allocation, nothing to destroy,
    /// and with no index it covers every sub-material on the renderer - including a
    /// ParticleSystemRenderer's separate trail material.
    /// </summary>
    public static void Tint(Renderer r, Color hdrColor)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", hdrColor);
        mpb.SetColor("_Color", hdrColor);   // Sprites/Default fallback uses _Color
        r.SetPropertyBlock(mpb);
    }

    // A soft radial dot: white, with the falloff in the alpha channel so _BaseColor and the
    // particle's startColor tint it live - the same trick as DroneHUD.BuildVignetteSprite. The
    // falloff is squared to keep a hot core and a thin halo; a linear ramp reads as fog. 64 px is
    // plenty, because bilinear filtering is what turns texels into a glow.
    static Texture2D BuildSoftDot(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp; // alpha is 0 at the rim, so the quad edge never shows
        tex.name = "FX_SoftDot";
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] px = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // A cross-section band for LineRenderer. LineRenderer maps U along the line and V across its
    // width, so the falloff has to run in V - i.e. down the texture's *height*. Four texels of width
    // is enough; there is nothing to vary along U.
    static Texture2D BuildSoftBand(int height)
    {
        const int Width = 4;

        Texture2D tex = new Texture2D(Width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "FX_SoftBand";
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] px = new Color[Width * height];

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;

            // 1 down the spine, 0 at both edges, squared so the core stays a hard filament and only
            // the outside blurs. That is what makes a straight quad read as an arc under bloom.
            float a = 1f - Mathf.Abs(v * 2f - 1f);
            a = a * a;

            for (int x = 0; x < Width; x++) px[y * Width + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
