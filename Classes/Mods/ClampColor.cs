/*
 * ii Reborn
 * Portions Copyright (C) 2025–2026 Goldentrophy Software
 * Licensed under GNU GPL v3.0-or-later — see LICENSE and NOTICE.
 * This file is part of a derivative work; see NOTICE for attribution
 * and modification history. Do not remove this notice.
 */

﻿using iiMenu.Classes.Menu;
using UnityEngine;

namespace iiMenu.Classes.Mods
{
    public class ClampColor : MonoBehaviour
    {
        public void Start()
        {
            targetRenderer.gameObject.GetComponent<ColorChanger>()?.Start();

            gameObjectRenderer = GetComponent<Renderer>();
            Update();
        }

        public void Update()
        {
            // Both renderers' .material are native property accesses and this runs every
            // frame on every ClampColor, so hold the references and only write on change.
            if (ownMaterial == null)
                ownMaterial = gameObjectRenderer.material;

            Material targetMaterial = targetRenderer.material;

            if (ownMaterial.shader != targetMaterial.shader)
            {
                ownMaterial = new Material(targetMaterial.shader);
                gameObjectRenderer.material = ownMaterial;
            }

            if (targetMaterial.mainTexture != null && ownMaterial.mainTexture != targetMaterial.mainTexture)
                ownMaterial.mainTexture = targetMaterial.mainTexture;

            if (ownMaterial.color != targetMaterial.color)
                ownMaterial.color = targetMaterial.color;
        }

        public Renderer gameObjectRenderer;
        public Renderer targetRenderer;

        private Material ownMaterial;
    }
}
