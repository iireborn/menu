/*
 * ii Reborn
 * Portions Copyright (C) 2025–2026 Goldentrophy Software
 * Licensed under GNU GPL v3.0-or-later — see LICENSE and NOTICE.
 * This file is part of a derivative work; see NOTICE for attribution
 * and modification history. Do not remove this notice.
 */

using GorillaExtensions;
using iiMenu.Menu;
using UnityEngine;

namespace iiMenu.Classes.Menu
{
    public class ColorChanger : MonoBehaviour
    {
        public void Start()
        {
            if (colors == null)
            {
                Destroy(this);
                return;
            }

            targetRenderer = GetComponent<Renderer>();

            if (colors.IsFlat())
            {
                Update();
                Destroy(this);
                return;
            }
            
            Update();
        }

        public void Update()
        {
            targetRenderer.enabled = overrideTransparency ?? !colors.transparent;

            if (colors.transparent)
                return;

            // Renderer.material is a native property access, and this Update runs on
            // every ColorChanger every frame, so the material reference is held onto
            // instead of being re-fetched, and colour is only written when it differs.
            Material material = cachedMaterial;

            if (material == null)
            {
                material = targetRenderer.material;
                cachedMaterial = material;
            }

            if (!Main.dynamicGradients)
            {
                Color color = colors.GetCurrentColor();

                if (material.color != color)
                    material.color = color;
            }
            else if (colors.IsFlat())
            {
                Color color = colors.GetColor(0);

                if (material.color != color)
                    material.color = color;
            }
            else if (!gradientMaterialReady)
            {
                if (material.shader.name != "Universal Render Pipeline/Unlit" && material.mainTexture == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                    {
                        mainTexture = Main.GetGradientTexture(colors.GetColor(0), colors.GetColor(1))
                    };

                    targetRenderer.material = material;
                    cachedMaterial = material;

                    if (Main.scrollingGradients)
                        gameObject.GetOrAddComponent<ScrollMaterial>();
                }

                gradientMaterialReady = true;
            }

            if (!Main.transparentMenu) return;

            Color faded = material.color;
            faded.a = 0.5f;

            if (material.color != faded)
                material.color = faded;
        }

        public Renderer targetRenderer;
        public ExtGradient colors;
        public bool? overrideTransparency;

        private Material cachedMaterial;
        private bool gradientMaterialReady;
    }
}
