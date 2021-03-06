﻿// <copyright file="AwesomeInvnetoryMod.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AwesomeInventory.UI;
using UnityEngine;
using Verse;

namespace AwesomeInventory
{
    /// <summary>
    /// A dialog window for configuring mod settings.
    /// </summary>
    public class AwesomeInvnetoryMod : Mod
    {
        private static Vector2 _qualityColorScrollListHeight;
        private static Rect _qualityColorViewRect;
        private static List<QualityColor> _themes;

        private static AwesomeInventorySetting _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwesomeInvnetoryMod"/> class.
        /// </summary>
        /// <param name="content"> Includes metadata of a mod. </param>
        public AwesomeInvnetoryMod(ModContentPack content)
            : base(content)
        {
            _settings = this.GetSettings<AwesomeInventorySetting>();
        }

        /// <summary>
        /// Gets setting for Awesome inventory.
        /// </summary>
        public static AwesomeInventorySetting Settings
        {
            get => _settings;
        }

        /// <summary>
        /// Draw content in window.
        /// </summary>
        /// <param name="inRect"> Rect for drawing. </param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.ColumnWidth = inRect.width / 3 - 20f;
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled(UIText.UseLoadout.TranslateSimple(), ref _settings.UseLoadout, UIText.UseLoadoutTooltip.TranslateSimple());
            listingStandard.CheckboxLabeled(UIText.AutoEquipWeapon.TranslateSimple(), ref _settings.AutoEquipWeapon, UIText.AutoEquipWeaponTooltip.TranslateSimple());
            listingStandard.CheckboxLabeled(UIText.UseGearTabToggle.TranslateSimple(), ref _settings.UseToggleGizmo, UIText.UseGearTabToggleTooltip.TranslateSimple());

            listingStandard.NewColumn();
            this.DrawQualityColorScrollableList(listingStandard);
            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Return the name for display in the game's mod setting section.
        /// </summary>
        /// <returns> Display name for Awesome Inventory. </returns>
        public override string SettingsCategory()
        {
            return UIText.AwesomeInventoryDisplayName.TranslateSimple();
        }

        private void DrawQualityColorScrollableList(Listing_Standard listingStandard)
        {
            Rect labelRect = listingStandard.Label("Choose theme for quality color: ");
            Widgets.DrawLineHorizontal(labelRect.x, listingStandard.CurHeight, labelRect.width - GenUI.ScrollBarWidth);

            Rect outRect = listingStandard.GetRect(GenUI.ListSpacing * 4);

            if (_themes == null)
            {
                _themes = AwesomeInventoryServiceProvider.Plugins.Values.OfType<QualityColor>().ToList();
                float height = _themes.Count * GenUI.ListSpacing;
                _qualityColorViewRect = outRect.ReplaceHeight(Mathf.Max(outRect.height + GenUI.ListSpacing, height));
            }

            Widgets.BeginScrollView(outRect, ref _qualityColorScrollListHeight, _qualityColorViewRect.AtZero());

            float rollingY = 0;
            Rect optionRect = new Rect(0, 0, _qualityColorViewRect.width - GenUI.ScrollBarWidth, GenUI.ListSpacing);
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (QualityColor qualityColor in _themes)
            {
                optionRect = optionRect.ReplaceY(rollingY);
                if (Widgets.RadioButtonLabeled(optionRect, qualityColor.DisplayName, _settings.QualityColorPluginID == qualityColor.ID))
                {
                    _settings.QualityColorPluginID = qualityColor.ID;
                    QualityColor.ChangeTheme(qualityColor.ID);
                }

                Widgets.DrawHighlightIfMouseover(optionRect);
                TooltipHandler.TipRegion(optionRect, this.TooltipForQualityColor(qualityColor));

                rollingY += GenUI.ListSpacing;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndScrollView();
        }

        private string TooltipForQualityColor(QualityColor qualityColor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Legendary".Colorize(qualityColor.Legendary));
            stringBuilder.AppendLine("Masterwork".Colorize(qualityColor.Masterwork));
            stringBuilder.AppendLine("Excellent".Colorize(qualityColor.Excellent));
            stringBuilder.AppendLine("Good".Colorize(qualityColor.Good));
            stringBuilder.AppendLine("Normal".Colorize(qualityColor.Normal));
            stringBuilder.AppendLine("Poor".Colorize(qualityColor.Poor));
            stringBuilder.AppendLine("Awful".Colorize(qualityColor.Awful));
            stringBuilder.AppendLine("Generic".Colorize(qualityColor.Generic));

            return stringBuilder.ToString();
        }
    }
}
