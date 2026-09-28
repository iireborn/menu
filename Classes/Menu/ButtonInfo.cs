/*
 * ii Reborn
 * Portions Copyright (C) 2025–2026 Goldentrophy Software
 * Licensed under GNU GPL v3.0-or-later — see LICENSE and NOTICE.
 * This file is part of a derivative work; see NOTICE for attribution
 * and modification history. Do not remove this notice.
 */

﻿using System;

namespace iiMenu.Classes.Menu
{
    public class ButtonInfo
    {
        public string buttonText = "-"; // Button code name, displayed in menu if overlapText is null
        public string overlapText;      // Button text displayed on menu, overrides buttonText

        public string[] aliases;        // Other terms a button can go by for searching, not displayed in menu

        public string toolTip = "This button doesn't have a tooltip/tutorial."; // Tooltip of button, a short description

        public Action method;           // Every frame before GTPlayer.LateUpdate is called
        public Action postMethod;       // Every frame after GTPlayer.LateUpdate is called

        public Action enableMethod;     // Once before method on enable
        public Action disableMethod;    // Once on disable

        public bool enabled;
        public bool isTogglable = true;

        public bool label;
        public bool incremental;
        public bool detected;

        public bool hideFromArraylist;

        public string customBind;
        public string rebindKey;
    }
}
