/*
 * ii Reborn
 * Portions Copyright (C) 2025–2026 Goldentrophy Software
 * Licensed under GNU GPL v3.0-or-later — see LICENSE and NOTICE.
 * This file is part of a derivative work; see NOTICE for attribution
 * and modification history. Do not remove this notice.
 */

﻿using UnityEngine;
using UnityEngine.UI;

namespace iiMenu.Classes.Mods
{
    public class ClampText : MonoBehaviour
    {
        public void Start()
        {
            currentText = GetComponent<Text>();
            LateUpdate();
        }

        public void LateUpdate()
        {
            if (currentText == null || targetText == null)
                return;

            if (currentText.text != targetText.text)
                currentText.text = targetText.text;
        }

        public Text currentText;
        public Text targetText;
    }
}
