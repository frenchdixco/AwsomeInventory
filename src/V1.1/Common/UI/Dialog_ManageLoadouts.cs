﻿// <copyright file="Dialog_ManageLoadouts.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeInventory.Loadout;
using AwesomeInventory.Resources;
using RimWorld;
using RPG_Inventory_Remake_Common;
using UnityEngine;
using Verse;

namespace AwesomeInventory.UI
{
    /// <summary>
    /// A dialog window for managing loadouts.
    /// </summary>
    public class Dialog_ManageLoadouts : Window, IReset
    {
        private const float _paneDivider = 5 / 9f;
        private const int _loadoutNameMaxLength = 50;
        private static readonly HashSet<ThingDef> _allSuitableDefs = DefManager.SuitableDefs;

        /// <summary>
        /// Controls the window size and position.
        /// </summary>
        private static Vector2 _initialSize;
        private static Pawn _pawn;
        private static List<ItemContext> _itemContexts;
        private static List<IReset> _resettables = new List<IReset>();

        private Vector2 _sourceListScrollPosition = Vector2.zero;
        private AwesomeInventoryLoadout _currentLoadout;
        private string _filter = string.Empty;
        private float _scrollViewHeight;
        private Vector2 _loadoutListScrollPosition = Vector2.zero;
        private List<ItemContext> _categorySource;
        private List<ItemContext> _source;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Dialog_ManageLoadouts"/> class.
        /// </summary>
        /// <param name="loadout"> Selected loadout. </param>
        /// <param name="pawn"> Selected pawn. </param>
        public Dialog_ManageLoadouts(AwesomeInventoryLoadout loadout, Pawn pawn)
        {
            ValidateArg.NotNull(loadout, nameof(loadout));
            _pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));

            float width = GenUI.GetWidthCached(UIText.TenCharsString.Times(10));
            _initialSize = new Vector2(width, Verse.UI.screenHeight / 2f);

            _currentLoadout = loadout;
            _resettables.Add(this);
            _resettables.Add(new WhiteBlacklistView());

            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
        }

        #endregion Constructors

        /// <summary>
        /// Source categories for loadout dialog.
        /// </summary>
        public enum CategorySelection
        {
            /// <summary>
            /// Ranged weapons.
            /// </summary>
            Ranged,

            /// <summary>
            /// Melee weapons.
            /// </summary>
            Melee,

            /// <summary>
            /// Apparels.
            /// </summary>
            Apparel,

            /// <summary>
            /// Things that are minified.
            /// </summary>
            Minified,

            /// <summary>
            /// Generic thing def.
            /// </summary>
            Generic,

            /// <summary>
            /// All things, won't include generics, can include minified/able now.
            /// </summary>
            All,
        }

        /// <summary>
        /// Gets initial window size for this dialog.
        /// </summary>
        public override Vector2 InitialSize => _initialSize;

        #region Methods

        /// <summary>
        /// Draw contents in window.
        /// </summary>
        /// <param name="canvas"> Canvas to draw. </param>
        public override void DoWindowContents(Rect canvas)
        {
            /*
             * ||        Buttons          ||
             * || Loadout Name Text Field ||    Category Button Image   ||
             * ||     Items in Loadout    || Avaialable Items to Choose ||
             * ||        Weight Bar       ||
             *
             */

            if (Event.current.type == EventType.Layout)
                return;

            if (Find.Selector.SingleSelectedThing is Pawn pawn && pawn.IsColonist && !pawn.Dead)
            {
                if (pawn != _pawn)
                {
                    _pawn = pawn;
                    AwesomeInventoryLoadout loadout = _pawn.GetLoadout();
                    if (loadout == null)
                    {
                        loadout = new AwesomeInventoryLoadout(pawn);
                        LoadoutManager.AddLoadout(loadout);
                        pawn.SetLoadout(loadout);
                    }

                    _currentLoadout = loadout;
                    _resettables.ForEach(r => r.Reset());
                }
            }
            else
            {
                this.Close();
            }

            GUI.BeginGroup(canvas);
            Text.Font = GameFont.Small;
            Vector2 useableSize = new Vector2(
                canvas.width - DrawUtility.CurrentPadding * 2,
                canvas.height - DrawUtility.CurrentPadding * 2);

            // Draw top buttons
            WidgetRow buttonRow = new WidgetRow(useableSize.x, 0, UIDirection.LeftThenDown, useableSize.x);
            this.DrawTopButtons(buttonRow);

            // Set up rects for the rest parts.
            Rect nameFieldRect = new Rect(
                0,
                0,
                useableSize.x - buttonRow.FinalX - GenUI.GapWide,
                GenUI.SmallIconSize);

            Rect whiteBlackListRect = new Rect(
                0,
                nameFieldRect.yMax + GenUI.GapTiny,
                useableSize.x * _paneDivider,
                GenUI.SmallIconSize);

            Rect loadoutItemsRect = new Rect(
                0,
                whiteBlackListRect.yMax + GenUI.GapTiny,
                whiteBlackListRect.width,
                useableSize.y - whiteBlackListRect.yMax - GenUI.ListSpacing);

            Rect sourceButtonRect = new Rect(
                loadoutItemsRect.xMax + Listing.ColumnSpacing,
                buttonRow.FinalY + GenUI.ListSpacing,
                useableSize.x - loadoutItemsRect.xMax - Listing.ColumnSpacing,
                GenUI.SmallIconSize);

            Rect sourceItemsRect = new Rect(
                sourceButtonRect.x,
                sourceButtonRect.yMax + GenUI.GapTiny,
                sourceButtonRect.width,
                useableSize.y - sourceButtonRect.yMax);

            Rect weightBarRect = new Rect(
                0,
                canvas.yMax - DrawUtility.CurrentPadding - GenUI.SmallIconSize,
                loadoutItemsRect.width,
                GenUI.SmallIconSize);

            this.DrawWhiteBlackListOptions(whiteBlackListRect);
            this.DrawLoadoutNameField(nameFieldRect);

            if (WhiteBlacklistView.IsWishlist)
                this.DrawItemsInLoadout(loadoutItemsRect, _currentLoadout);
            else
                this.DrawItemsInLoadout(loadoutItemsRect, _currentLoadout.BlackList);

            GUI.DrawTexture(new Rect(loadoutItemsRect.x, loadoutItemsRect.yMax, loadoutItemsRect.width, 1f), BaseContent.GreyTex);
            this.DrawWeightBar(weightBarRect);

            this.DrawCategoryIcon(sourceButtonRect);
            this.DrawItemsInSourceCategory(sourceItemsRect);

            GUI.EndGroup();
        }

        /// <summary>
        /// Called by game root code before the window is opened.
        /// </summary>
        /// <remarks> It is only called once for the entire time period when this dialog is open including a change in selected pawn. </remarks>
        public override void PreOpen()
        {
            base.PreOpen();
            HashSet<ThingDef> visibleDefs = new HashSet<ThingDef>(_allSuitableDefs);
            visibleDefs.IntersectWith(
                Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEverOrMinifiable).Select(t => t.def));

            _itemContexts = new List<ItemContext>();
            foreach (ThingDef td in _allSuitableDefs)
            {
                ItemContext itemContext = new ItemContext() { ThingDef = td };
                if (td is AIGenericDef genericDef)
                {
                    itemContext.IsVisible = !visibleDefs.Any(def => genericDef.Includes(def));
                }
                else
                {
                    itemContext.IsVisible = !visibleDefs.Contains(td);
                }

                _itemContexts.Add(itemContext);
            }

            _itemContexts = _itemContexts.OrderBy(td => td.ThingDef.label).ToList();
            SetCategory(CategorySelection.Ranged);
        }

        /// <summary>
        /// Draw icon for source category.
        /// </summary>
        /// <param name="canvas"> <see cref="Rect"/> for drawing. </param>
        public void DrawCategoryIcon(Rect canvas)
        {
            WidgetRow row = new WidgetRow(canvas.x, canvas.y);
            DrawCategoryIcon(CategorySelection.Ranged, TexResource.IconRanged, ref row, UIText.SourceRangedTip.TranslateSimple());
            DrawCategoryIcon(CategorySelection.Melee, TexResource.IconMelee, ref row, UIText.SourceMeleeTip.TranslateSimple());
            DrawCategoryIcon(CategorySelection.Apparel, TexResource.Apparel, ref row, UIText.SourceApparelTip.TranslateSimple());
            DrawCategoryIcon(CategorySelection.Minified, TexResource.IconMinified, ref row, UIText.SourceMinifiedTip.TranslateSimple());
            DrawCategoryIcon(CategorySelection.Generic, TexResource.IconGeneric, ref row, UIText.SourceGenericTip.TranslateSimple());
            DrawCategoryIcon(CategorySelection.All, TexResource.IconAll, ref row, UIText.SourceAllTip.TranslateSimple());

            float nameFieldLen = GenUI.GetWidthCached(UIText.TenCharsString);
            float incrementX = canvas.xMax - row.FinalX - nameFieldLen - WidgetRow.IconSize - WidgetRow.ButtonExtraSpace;
            row.Gap(incrementX);
            row.Icon(TexResource.IconSearch, UIText.SourceFilterTip.TranslateSimple());

            Rect textFilterRect = new Rect(row.FinalX, canvas.y, nameFieldLen, canvas.height);
            this.DrawTextFilter(textFilterRect);
            TooltipHandler.TipRegion(textFilterRect, UIText.SourceFilterTip.TranslateSimple());
        }

        /// <summary>
        /// Set category for drawing available items in selection.
        /// </summary>
        /// <param name="category"> Category for selection. </param>
        public void SetCategory(CategorySelection category)
        {
            switch (category)
            {
                case CategorySelection.Ranged:
                    _source = _categorySource = _itemContexts.Where(context => context.ThingDef.IsRangedWeapon).ToList();
                    break;

                case CategorySelection.Melee:
                    _source = _categorySource = _itemContexts.Where(context => context.ThingDef.IsMeleeWeapon).ToList();
                    break;

                case CategorySelection.Apparel:
                    _source = _categorySource = _itemContexts.Where(context => context.ThingDef.IsApparel).ToList();
                    break;

                case CategorySelection.Minified:
                    _source = _categorySource = _itemContexts.Where(context => context.ThingDef.Minifiable).ToList();
                    break;

                case CategorySelection.Generic:
                    _source = _categorySource = _itemContexts.Where(context => context.ThingDef is AIGenericDef).ToList();
                    break;

                case CategorySelection.All:
                default:
                    _source = _categorySource = _itemContexts;
                    break;
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _sourceListScrollPosition = Vector2.zero;
            _loadoutListScrollPosition = Vector2.zero;
        }

        /// <summary>
        /// Draw wishlist/blacklist choice on screen.
        /// </summary>
        /// <param name="canvas"> Rect for drawing. </param>
        protected virtual void DrawWhiteBlackListOptions(Rect canvas)
        {
            Rect drawingRect = new Rect(0, canvas.y, GenUI.SmallIconSize * 2 + DrawUtility.TwentyCharsWidth + GenUI.GapTiny * 2, GenUI.ListSpacing);
            Rect centeredRect = drawingRect.CenteredOnXIn(canvas);
            WidgetRow widgetRow = new WidgetRow(centeredRect.x, centeredRect.y, UIDirection.RightThenDown);

            if (widgetRow.ButtonIcon(TexResource.TriangleLeft))
                WhiteBlacklistView.IsWishlist ^= true;

#pragma warning disable SA1118 // Parameter should not span multiple lines
            Text.Anchor = TextAnchor.MiddleCenter;
            widgetRow.LabelWithHighlight(
                WhiteBlacklistView.IsWishlist
                    ? WhiteBlacklistView.WishlistDisplayName
                    : WhiteBlacklistView.BlacklistDisplayName
                , WhiteBlacklistView.IsWishlist
                    ? UIText.WishlistTooltip.TranslateSimple()
                    : UIText.BlacklistTooltip.TranslateSimple()
                , DrawUtility.TwentyCharsWidth);
            Text.Anchor = TextAnchor.UpperLeft;
#pragma warning restore SA1118 // Parameter should not span multiple lines

            if (widgetRow.ButtonIcon(TexResource.TriangleRight))
                WhiteBlacklistView.IsWishlist ^= true;
        }

        /// <summary>
        /// Draw top buttons in <see cref="Dialog_ManageLoadouts"/>.
        /// </summary>
        /// <param name="row"> Helper for drawing. </param>
        protected virtual void DrawTopButtons(WidgetRow row)
        {
            ValidateArg.NotNull(row, nameof(row));

            List<AwesomeInventoryLoadout> loadouts = LoadoutManager.Loadouts;

            if (row.ButtonText(UIText.DeleteLoadout.TranslateSimple()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                for (int j = 0; j < loadouts.Count; j++)
                {
                    int i = j;
                    options.Add(new FloatMenuOption(
                        loadouts[i].label,
                        () =>
                        {
                            if (loadouts.Count > 1)
                            {
                                LoadoutManager.TryRemoveLoadout(loadouts[i], false);
                            }
                            else
                            {
                                Rect msgRect = new Rect(Vector2.zero, Text.CalcSize(ErrorMessage.TryToDeleteLastLoadout.Translate()))
                                                .ExpandedBy(50);
                                Find.WindowStack.Add(
                                    new Dialog_InstantMessage(
                                        ErrorMessage.TryToDeleteLastLoadout.Translate(), msgRect.size, UIText.OK.Translate())
                                    {
                                        windowRect = msgRect,
                                    });
                            }
                        }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (row.ButtonText(UIText.CopyLoadout.TranslateSimple()))
            {
                _currentLoadout = new AwesomeInventoryLoadout(_currentLoadout);
                LoadoutManager.AddLoadout(_currentLoadout);
                _pawn.SetLoadout(_currentLoadout);
            }

            if (row.ButtonText(UIText.NewLoadout.TranslateSimple()))
            {
                AwesomeInventoryLoadout loadout = new AwesomeInventoryLoadout(_pawn)
                {
                    label = LoadoutManager.GetIncrementalLabel(_currentLoadout.label),
                };
                LoadoutManager.AddLoadout(loadout);
                _currentLoadout = loadout;
                _pawn.SetLoadout(loadout);
            }

            if (row.ButtonText(UIText.SelectLoadout.TranslateSimple()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                if (loadouts.Count == 0)
                {
                    options.Add(new FloatMenuOption(UIText.NoLoadout.Translate(), null));
                }
                else
                {
                    for (int j = 0; j < loadouts.Count; j++)
                    {
                        int i = j;
                        options.Add(new FloatMenuOption(
                            loadouts[i].label,
                            () =>
                            {
                                _currentLoadout = loadouts[i];
                                _loadoutListScrollPosition = Vector2.zero;
                                _pawn.SetLoadout(_currentLoadout);
                            }));
                    }
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        /// <summary>
        /// Draw item information in a row.
        /// </summary>
        /// <param name="row"> Rect used for drawing. </param>
        /// <param name="index"> Position of a <see cref="ThingGroupSelector"/> in <paramref name="groupSelectors"/>. </param>
        /// <param name="groupSelectors"> Thing to draw. </param>
        /// <param name="reorderableGroup"> The group this <paramref name="row"/> belongs to. </param>
        /// <param name="drawShadow"> If true, it draws a shadow copy of the row. It is used for drawing a row when it is dragged. </param>
        protected virtual void DrawItemRow(Rect row, int index, IList<ThingGroupSelector> groupSelectors, int reorderableGroup, bool drawShadow = false)
        {
            ValidateArg.NotNull(row, nameof(row));
            ValidateArg.NotNull(groupSelectors, nameof(groupSelectors));

            /* Label (fill) | Weight | Gear Icon | Count Field | Delete Icon */

            WidgetRow widgetRow = new WidgetRow(row.width, row.y, UIDirection.LeftThenDown, row.width);
            ThingGroupSelector groupSelector = groupSelectors[index];

            // Draw delete icon.
            if (widgetRow.ButtonIcon(TexResource.CloseXSmall, UIText.Delete.TranslateSimple()))
            {
                groupSelectors.RemoveAt(index);
            }

            Text.Anchor = TextAnchor.MiddleLeft;

            // Draw count field.
            if (WhiteBlacklistView.IsWishlist)
            {
                this.DrawCountField(
                new Rect(widgetRow.FinalX - WidgetRow.IconSize * 2 - WidgetRow.DefaultGap, widgetRow.FinalY, WidgetRow.IconSize * 2, GenUI.ListSpacing),
                groupSelector);
                widgetRow.GapButtonIcon();
                widgetRow.GapButtonIcon();
            }

            // Draw gear icon.
            ThingDef allowedThing = groupSelector.AllowedThing;
            if ((allowedThing.MadeFromStuff || allowedThing.HasComp(typeof(CompQuality)) || allowedThing.useHitPoints)
                && widgetRow.ButtonIcon(TexResource.Gear))
            {
                Find.WindowStack.Add(new Dialog_StuffAndQuality(groupSelector));
            }

            Text.WordWrap = false;

            // Draw weight.
            widgetRow.Label(groupSelector.Weight.ToStringMass());

            // Draw label.
            Rect labelRect = widgetRow.Label(
                drawShadow
                    ? groupSelector.LabelCapNoCount.StripTags().Colorize(Theme.MilkySlicky.ForeGround)
                    : groupSelector.LabelCapNoCount
                , widgetRow.FinalX);

            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;

            if (!drawShadow)
            {
                ReorderableWidget.Reorderable(reorderableGroup, labelRect);

                // Tooltips && Highlights
                Widgets.DrawHighlightIfMouseover(row);
                if (Event.current.type == EventType.MouseDown)
                {
                    TooltipHandler.ClearTooltipsFrom(labelRect);
                }
                else
                {
                    TooltipHandler.TipRegion(labelRect, UIText.DragToReorder.Translate());
                }
            }
        }

        /// <summary>
        /// Check if <paramref name="loadout"/> is too heavy for <paramref name="pawn"/>.
        /// </summary>
        /// <param name="pawn"> The pawn used for baseline. </param>
        /// <param name="loadout"> Loadout to check. </param>
        /// <returns> Returns true if <paramref name="loadout"/> is too heavy for <paramref name="pawn"/>. </returns>
        protected virtual bool IsOverEncumbered(Pawn pawn, AwesomeInventoryLoadout loadout)
        {
            ValidateArg.NotNull(loadout, nameof(loadout));

            return loadout.Weight / MassUtility.Capacity(pawn) > 1f;
        }

        /// <summary>
        /// Draw weight bar at the bottom of the loadout window.
        /// </summary>
        /// <param name="canvas"> Rect for drawing. </param>
        protected virtual void DrawWeightBar(Rect canvas)
        {
            float fillPercent = Mathf.Clamp01(_currentLoadout.Weight / MassUtility.Capacity(_pawn));
            GenBar.BarWithOverlay(
                canvas,
                fillPercent,
                this.IsOverEncumbered(_pawn, _currentLoadout) ? AwesomeInventoryTex.ValvetTex as Texture2D : AwesomeInventoryTex.RMPrimaryTex as Texture2D,
                UIText.Weight.Translate(),
                _currentLoadout.Weight.ToString("0.#") + "/" + MassUtility.Capacity(_pawn).ToStringMass(),
                string.Empty);
        }

        private static void ReorderItems(int oldIndex, int newIndex, IList<ThingGroupSelector> groupSelectors)
        {
            if (oldIndex != newIndex)
            {
                groupSelectors.Insert(newIndex, groupSelectors[oldIndex]);
                groupSelectors.RemoveAt((oldIndex >= newIndex) ? (oldIndex + 1) : oldIndex);
            }
        }

        private void DrawCountField(Rect canvas, ThingGroupSelector groupSelector)
        {
            int countInt = groupSelector.AllowedStackCount;
            string buffer = countInt.ToString();
            Widgets.TextFieldNumeric(canvas, ref countInt, ref buffer);
            TooltipHandler.TipRegion(canvas, UIText.CountFieldTip.Translate(groupSelector.AllowedStackCount));

            if (countInt != groupSelector.AllowedStackCount)
            {
                groupSelector.SetStackCount(countInt);
            }
        }

        private void DrawTextFilter(Rect canvas)
        {
            string filter = GUI.TextField(canvas, _filter);
            if (filter != _filter)
            {
                _filter = filter;
                _source = _categorySource.Where(td => td.ThingDef.label.ToUpperInvariant().Contains(_filter.ToUpperInvariant())).ToList();
            }
        }

        private void DrawLoadoutNameField(Rect canvas)
        {
            _currentLoadout.label = Widgets.TextField(canvas, _currentLoadout.label, _loadoutNameMaxLength, Outfit.ValidNameRegex);
        }

        private void DrawItemsInLoadout(Rect canvas, IList<ThingGroupSelector> groupSelectors)
        {
            Rect listRect = new Rect(0, 0, canvas.width - GenUI.ScrollBarWidth, _scrollViewHeight);

            // darken whole area
            GUI.DrawTexture(canvas, TexResource.DarkBackground);
            Widgets.BeginScrollView(canvas, ref _loadoutListScrollPosition, listRect);

            // Set up reorder functionality
            int reorderableGroup = ReorderableWidget.NewGroup(
                (int from, int to) =>
                {
                    ReorderItems(from, to, groupSelectors);
                    DrawUtility.ResetDrag();
                }
                , ReorderableDirection.Vertical
                , -1
                , (index, pos) =>
                {
                    Vector2 position = DrawUtility.GetPostionForDrag(windowRect.ContractedBy(Margin), new Vector2(canvas.x, canvas.y), index, GenUI.ListSpacing);
                    Rect dragRect = new Rect(position, new Vector2(listRect.width, GenUI.ListSpacing));
                    Find.WindowStack.ImmediateWindow(
                        Rand.Int,
                        dragRect,
                        WindowLayer.Super,
                        () =>
                        {
                            GUI.DrawTexture(dragRect.AtZero(), SolidColorMaterials.NewSolidColorTexture(Theme.MilkySlicky.BackGround));
                            this.DrawItemRow(dragRect.AtZero(), index, groupSelectors, 0, true);
                        }, false);
                });

            float curY = 0f;
            for (int i = 0; i < groupSelectors.Count; i++)
            {
                // create row rect
                Rect row = new Rect(0f, curY, listRect.width, GenUI.ListSpacing);
                curY += GenUI.ListSpacing;

                // alternate row background
                if (i % 2 == 0)
                    GUI.DrawTexture(row, TexResource.DarkBackground);

                this.DrawItemRow(row, i, groupSelectors, reorderableGroup);
                GUI.color = Color.white;
            }

            _scrollViewHeight = curY + GenUI.ListSpacing;

            Widgets.EndScrollView();
        }

        private void DrawItemsInSourceCategory(Rect canvas)
        {
            GUI.DrawTexture(canvas, TexResource.DarkBackground);

            Rect viewRect = new Rect(canvas);
            viewRect.height = _source.Count * GenUI.ListSpacing;
            viewRect.width -= GenUI.GapWide;

            Widgets.BeginScrollView(canvas, ref _sourceListScrollPosition, viewRect.AtZero());
            DrawUtility.GetIndexRangeFromScrollPosition(canvas.height, _sourceListScrollPosition.y, out int from, out int to, GenUI.ListSpacing);
            for (int i = from; i < to && i < _source.Count; i++)
            {
                Color baseColor = GUI.color;

                // gray out weapons not in stock
                if (_source[i].IsVisible)
                    GUI.color = Color.gray;

                Rect row = new Rect(0f, i * GenUI.ListSpacing, canvas.width, GenUI.ListSpacing);
                Rect labelRect = new Rect(row);
                TooltipHandler.TipRegion(row, _source[i].ThingDef.GetWeightAndBulkTip());

                labelRect.xMin += GenUI.GapTiny;
                if (i % 2 == 0)
                    GUI.DrawTexture(row, TexResource.DarkBackground);

                int j = i;
                DrawUtility.DrawLabelButton(
                    labelRect
                    , _source[j].ThingDef.LabelCap
                    , () =>
                    {
                        ThingDef thingDef = _source[j].ThingDef;
                        ThingGroupSelector groupSelector = new ThingGroupSelector(thingDef);

                        ThingSelector thingSelector;
                        if (thingDef is AIGenericDef genericDef)
                        {
                            thingSelector = AwesomeInventoryServiceProvider.MakeInstanceOf<GenericThingSelector>(thingDef);
                        }
                        else
                        {
                            thingSelector = AwesomeInventoryServiceProvider.MakeInstanceOf<SingleThingSelector>(thingDef, null);
                        }

                        groupSelector.SetStackCount(1);
                        groupSelector.Add(thingSelector);

                        if (WhiteBlacklistView.IsWishlist)
                            _currentLoadout.Add(groupSelector);
                        else
                            _currentLoadout.AddToBlacklist(groupSelector);
                    });

                GUI.color = baseColor;
            }

            Widgets.EndScrollView();
        }

        private void DrawCategoryIcon(CategorySelection sourceSelected, Texture2D texButton, ref WidgetRow row, string tip)
        {
            if (row.ButtonIcon(texButton, tip))
            {
                SetCategory(sourceSelected);
                _filter = string.Empty;
                _sourceListScrollPosition = Vector2.zero;
            }
        }

        #endregion Methods

        private class WhiteBlacklistView : IReset
        {
            public static readonly string WishlistDisplayName = UIText.Wishlist.TranslateSimple();

            public static readonly string BlacklistDisplayName = UIText.Blacklist.TranslateSimple();

            public static bool IsWishlist { get; set; } = true;

            public void Reset()
            {
                IsWishlist = true;
            }
        }

        private class ItemContext
        {
            public ThingDef ThingDef;
            public bool IsVisible;
        }
    }
}
