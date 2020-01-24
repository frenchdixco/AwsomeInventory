﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RPG_Inventory_Remake_Common;
using RPG_Inventory_Remake.Resources;
using System.Diagnostics.CodeAnalysis;

namespace RPG_Inventory_Remake.Loadout
{
    public enum SourceSelection
    {
        Ranged,
        Melee,
        Ammo,
        Minified,
        Generic,
        All // All things, won't include generics, can include minified/able now.
    }

    public class Dialog_ManageLoadouts : Window
    {
        #region Fields
        #region Static Fields

        private static Texture2D
            _arrowBottom = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/arrowBottom"),
            _arrowDown = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/arrowDown"),
            _arrowTop = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/arrowTop"),
            _arrowUp = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/arrowUp"),
            _darkBackground = SolidColorMaterials.NewSolidColorTexture(0f, 0f, 0f, .2f),
            _iconEdit = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/edit"),
            _iconClear = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/clear"),
            _iconAmmo = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/ammo"),
            _iconRanged = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/ranged"),
            _iconMelee = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/melee"),
            _iconMinified = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/minified"),
            _iconGeneric = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/generic"),
            _iconAll = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/all"),
            _iconAmmoAdd = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/ammoAdd"),
            _iconSearch = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/search"),
            _iconMove = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/move"),
            _iconPickupDrop = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/loadoutPickupDrop"),
            _iconDropExcess = ContentFinder<Texture2D>.Get("UI/Icons/RPGI_Loadout/loadoutDropExcess");

        //private static List<Thing> _currentLoadout.CachedList;
        private static Pawn _pawn;
        /// <summary>
        /// Controls the window size and position
        /// </summary>
        private static Vector2 _initialSize = new Vector2(650f, 650f);
        private static List<SelectableItem> _selectableItems;
        private readonly static HashSet<ThingDef> _allSuitableDefs;
        private static float _scrollViewHeight;

        #endregion Statis Fields

        private Vector2 _availableScrollPosition = Vector2.zero;
        private readonly int _loadoutNameMaxLength = 50;
        private const float _barHeight = 24f;
        private Vector2 _countFieldSize = new Vector2(40f, 24f);
        private RPGILoadout<Thing> _currentLoadout;
        private string _filter = "";
        private const float _iconSize = 16f;
        private const float _margin = 6f;
        private const float _topAreaHeight = 30f;
        private Vector2 _slotScrollPosition = Vector2.zero;
        private List<SelectableItem> _source;
        private List<LoadoutGenericDef> _sourceGeneric;
        private SourceSelection _sourceType = SourceSelection.Ranged;

        #endregion Fields

        #region Constructors

        static Dialog_ManageLoadouts()
        {
            _allSuitableDefs = GameComponent_DefManager.GetSuitableDefs();
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public Dialog_ManageLoadouts(RPGILoadout<Thing> loadout, Pawn pawn)
        {
            ThrowHelper.ArgumentNullException(loadout, pawn);

            _currentLoadout = loadout;
            _pawn = pawn;
            //_currentLoadout.CachedList = _currentLoadout.CachedList;

            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        #endregion Constructors

        #region Properties
        public override Vector2 InitialSize => _initialSize;

        #endregion Properties

        #region Methods

        public override void DoWindowContents(Rect canvas)
        {
            GUI.BeginGroup(canvas);
            Text.Font = GameFont.Small;

            // SET UP RECTS
            // top buttons
            Rect selectRect = new Rect(0f, 0f, canvas.width * .2f, _topAreaHeight);
            Rect newRect = new Rect(selectRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight);
            Rect copyRect = new Rect(newRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight);
            Rect deleteRect = new Rect(copyRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight);

            // main areas
            Rect nameRect = new Rect(
                0f,
                _topAreaHeight + _margin * 2,
                (canvas.width - _margin) / 2f,
                24f);

            Rect slotListRect = new Rect(
                0f,
                nameRect.yMax + _margin,
                (canvas.width - _margin) / 2f,
                canvas.height - _topAreaHeight - nameRect.height - _barHeight * 2 - _margin * 5);

            Rect weightBarRect = new Rect(slotListRect.xMin, slotListRect.yMax + _margin, slotListRect.width, _barHeight);

            Rect sourceButtonRect = new Rect(
                slotListRect.xMax + _margin,
                _topAreaHeight + _margin * 2,
                (canvas.width - _margin) / 2f,
                24f);

            Rect selectionRect = new Rect(
                slotListRect.xMax + _margin,
                sourceButtonRect.yMax + _margin,
                (canvas.width - _margin) / 2f,
                canvas.height - 24f - _topAreaHeight - _margin * 3);

            List<RPGILoadout<Thing>> loadouts = LoadoutManager.Loadouts;
            // DRAW CONTENTS
            // buttons
            // select loadout
            if (Widgets.ButtonText(selectRect, "Corgi_SelectLoadout".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                if (loadouts.Count == 0)
                    options.Add(new FloatMenuOption("Corgi_NoLoadouts".Translate(), null));
                else
                {
                    for (int j = 0; j < loadouts.Count; j++)
                    {
                        int i = j;
                        options.Add(new FloatMenuOption(loadouts[i].Label,
                                                        delegate
                                                        {
                                                            _currentLoadout = loadouts[i];
                                                            _slotScrollPosition = Vector2.zero;
                                                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // create loadout
            if (Widgets.ButtonText(newRect, "Corgi_NewLoadout".Translate()))
            {
                RPGILoadout<Thing> loadout = new RPGILoadout<Thing>()
                {
                    Label = LoadoutManager.GetIncrementalLabel(_currentLoadout.Label)
                };
                LoadoutManager.AddLoadout(loadout);
                _currentLoadout = loadout;
                //_currentLoadout.CachedList = _currentLoadout.ToList();
            }

            // copy loadout
            if (_currentLoadout != null && Widgets.ButtonText(copyRect, "Corgi_CopyLoadout".Translate()))
            {
                _currentLoadout = new RPGILoadout<Thing>(_currentLoadout);
                LoadoutManager.AddLoadout(_currentLoadout);
                //_currentLoadout.CachedList = _currentLoadout.ToList();
            }

            // delete loadout
            if (Widgets.ButtonText(deleteRect, "Corgi_DeleteLoadout".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                for (int j = 0; j < loadouts.Count; j++)
                {
                    int i = j;
                    options.Add(new FloatMenuOption(loadouts[i].Label,
                        delegate
                        {
                            if (loadouts.Count > 1)
                            {
                                if (_currentLoadout == loadouts[i])
                                {
                                    LoadoutManager.RemoveLoadout(loadouts[i]);
                                    _currentLoadout = loadouts.First();
                                    //_currentLoadout.CachedList = _currentLoadout.ToList();
                                    return;
                                }
                                LoadoutManager.RemoveLoadout(loadouts[i]);
                            }
                            else
                            {
                                Rect msgRect = new Rect(Vector2.zero, Text.CalcSize(ErrorMessage.TryToDeleteLastLoadout.Translate()));
                                msgRect = msgRect.ExpandedBy(50);
                                Find.WindowStack.Add(
                                    new Dialog_InstantMessage
                                        (ErrorMessage.TryToDeleteLastLoadout.Translate(), msgRect.size, UIText.OK.Translate())
                                    {
                                        windowRect = msgRect
                                    });
                            }
                        }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // draw notification if no loadout selected
            if (_currentLoadout == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label(canvas, "Corgi_NoLoadoutSelected".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // and stop further drawing
                return;
            }

            DrawLoadoutTextField(nameRect);

            DrawCategoryIcon(sourceButtonRect);

            DrawItemsInCategory(selectionRect);

            DrawItemsInLoadout(slotListRect);

            // bars
            if (_currentLoadout != null)
            {
                UtilityLoadouts.DrawBar(weightBarRect, _currentLoadout.Weight, MassUtility.Capacity(_pawn), "Corgi_Weight".Translate(), null);
                // draw text overlays on bars
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightBarRect, _currentLoadout.Weight.ToString("0.#") + "/" + MassUtility.Capacity(_pawn).ToStringMass());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            // done!
            GUI.EndGroup();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            HashSet<ThingDef> visibleDefs = GameComponent_DefManager.GetSuitableDefs();
            visibleDefs.IntersectWith(Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEverOrMinifiable).Select(t => t.def));
            _selectableItems = new List<SelectableItem>();
            foreach (ThingDef td in _allSuitableDefs)
            {
                SelectableItem selectableItem = new SelectableItem() { thingDef = td };
                if (td is LoadoutGenericDef genericDef)
                {
                    selectableItem.isGreyedOut = visibleDefs.Any(def => genericDef.lambda(def));
                }
                else
                {
                    selectableItem.isGreyedOut = !visibleDefs.Contains(td);
                }
                _selectableItems.Add(selectableItem);
            }
            _selectableItems = _selectableItems.OrderBy(td => td.thingDef.label).ToList();
            SetSource(SourceSelection.Ranged);
        }

        public void DrawCategoryIcon(Rect canvas)
        {
            Rect button = new Rect(canvas.xMin, canvas.yMin + (canvas.height - 24f) / 2f, 24f, 24f);

            DrawSourceIcon(SourceSelection.Ranged, _iconRanged, ref button, "Corgi_SourceRangedTip".Translate());
            DrawSourceIcon(SourceSelection.Melee, _iconMelee, ref button, "Corgi_SourceMeleeTip".Translate());
            DrawSourceIcon(SourceSelection.Minified, _iconMinified, ref button, "Corgi_SourceMinifiedTip".Translate());
            DrawSourceIcon(SourceSelection.Generic, _iconGeneric, ref button, "Corgi_SourceGenericTip".Translate());
            DrawSourceIcon(SourceSelection.All, _iconAll, ref button, "Corgi_SourceAllTip".Translate());

            // filter input field
            Rect filterRect = new Rect(canvas.xMax - 75f, canvas.yMin + (canvas.height - 24f) / 2f, 75f, 24f);
            DrawFilterField(filterRect);
            TooltipHandler.TipRegion(filterRect, "Corgi_SourceFilterTip".Translate());

            // search icon
            button.x = filterRect.xMin - _margin * 2 - _iconSize;
            GUI.DrawTexture(button, _iconSearch);
            TooltipHandler.TipRegion(button, "Corgi_SourceFilterTip".Translate());

            // reset color
            GUI.color = Color.white;
        }

        public void FilterSource(string filter)
        {
            // reset source
            SetSource(_sourceType, true);

            // filter
            _source = _source.Where(td => td.thingDef.label.ToUpperInvariant().Contains(_filter.ToUpperInvariant())).ToList();
        }

        public void SetSource(SourceSelection source, bool preserveFilter = false)
        {
            _sourceGeneric = LoadoutGenericDef.GenericDefs;
            if (!preserveFilter)
                _filter = "";

            switch (source)
            {
                case SourceSelection.Ranged:
                    _source = _selectableItems.Where(row => row.thingDef.IsRangedWeapon).ToList();
                    _sourceType = SourceSelection.Ranged;
                    break;

                case SourceSelection.Melee:
                    _source = _selectableItems.Where(row => row.thingDef.IsMeleeWeapon).ToList();
                    _sourceType = SourceSelection.Melee;
                    break;

                case SourceSelection.Minified:
                    _source = _selectableItems.Where(row => row.thingDef.Minifiable).ToList();
                    _sourceType = SourceSelection.Minified;
                    break;

                case SourceSelection.Generic:
                    _sourceType = SourceSelection.Generic;
                    _source = _selectableItems.Where(row => row.thingDef is LoadoutGenericDef).ToList();
                    break;

                case SourceSelection.All:
                default:
                    _source = _selectableItems;
                    _sourceType = SourceSelection.All;
                    break;
            }
        }

        private void DrawCountField(Rect canvas, Thing thing)
        {
            if (thing == null)
                return;

            int countInt = thing.stackCount;
            string buffer = countInt.ToString();
            Widgets.TextFieldNumeric<int>(canvas, ref countInt, ref buffer);
            TooltipHandler.TipRegion(canvas, "Corgi_CountFieldTip".Translate(thing.stackCount));
            thing.stackCount = countInt;
        }

        private void DrawFilterField(Rect canvas)
        {
            string filter = GUI.TextField(canvas, _filter);
            if (filter != _filter)
            {
                _filter = filter;
                FilterSource(_filter);
            }
        }

        private void DrawLoadoutTextField(Rect canvas)
        {
            Widgets.TextField(canvas, _currentLoadout.Label, _loadoutNameMaxLength, Outfit.ValidNameRegex);
        }

        private void DrawSlot(Rect row, Thing thing, int reorderableGroup)
        {
            // set up rects
            // label (fill) | gear icon | count (50px) | delete (iconSize)

            Rect labelRect = new Rect(row);
            labelRect.xMax = row.xMax - row.height - _countFieldSize.x - _iconSize - GenUI.GapSmall;
            ReorderableWidget.Reorderable(reorderableGroup, labelRect);

            Rect gearIconRect = new Rect(labelRect.xMax, row.y, row.height, row.height);

            Rect countRect = new Rect(
                gearIconRect.xMax,
                row.yMin + (row.height - _countFieldSize.y) / 2f,
                _countFieldSize.x,
                _countFieldSize.y);

            Rect deleteRect = new Rect(countRect.xMax + GenUI.GapSmall, row.yMin + (row.height - _iconSize) / 2f, _iconSize, _iconSize);

            // interactions (main row rect)
            if (!Mouse.IsOver(deleteRect))
            {
                Widgets.DrawHighlightIfMouseover(row);
                TooltipHandler.TipRegion(row, string.Concat(thing.GetWeightTip(), '\n', UIText.DragToReorder));
            }

            // label
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(labelRect, thing.LabelCap);
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;

            // gear icon
            if (Widgets.ButtonImage(gearIconRect, TexButton.Gear))
            {
                Find.WindowStack.Add(new Dialog_StuffAndQuality(thing.def));
            }

            // count
            DrawCountField(countRect, thing);

            // delete
            if (Mouse.IsOver(deleteRect))
                GUI.DrawTexture(row, TexUI.HighlightTex);
            if (Widgets.ButtonImage(deleteRect, _iconClear))
            {
                _currentLoadout.RemoveItem(thing);
                //_currentLoadout.CachedList = _currentLoadout.ToList();
            }
            TooltipHandler.TipRegion(deleteRect, "Corgi_DeleteFilter".Translate());
        }

        private void DrawItemsInLoadout(Rect canvas)
        {
            // set up content canvas
            Rect listRect = new Rect(0, 0, canvas.width - GenUI.ScrollBarWidth, _scrollViewHeight);
            // darken whole area
            GUI.DrawTexture(canvas, _darkBackground);
            Widgets.BeginScrollView(canvas, ref _slotScrollPosition, listRect);
            // Set up reorder functionality
            int reorderableGroup = ReorderableWidget.NewGroup(
                delegate (int from, int to)
                {
                    ReorderItems(from, to);
                }
                , ReorderableDirection.Vertical);

            float curY = 0f;
            for (int i = 0; i < _currentLoadout.CachedList.Count; i++)
            {
                // create row rect
                Rect row = new Rect(0f, curY, listRect.width, GenUI.ListSpacing);
                curY += GenUI.ListSpacing;

                //// if we're dragging, and currently on this row, and this row is not the row being dragged - draw a ghost of the slot here
                //if (Dragging != null && Mouse.IsOver(row) && Dragging != _currentLoadout.Slots[i])
                //{
                //    // draw ghost
                //    GUI.color = new Color(.7f, .7f, .7f, .5f);
                //    DrawSlot(row, Dragging);
                //    GUI.color = Color.white;

                //    // catch mouseUp
                //    if (Input.GetMouseButtonUp(0))
                //    {
                //        _currentLoadout.MoveSlot(Dragging, i);
                //        Dragging = null;
                //    }

                //    // ofset further slots down
                //    row.y += GenUI.ListSpacing;
                //    curY += GenUI.ListSpacing;
                //}

                // alternate row background
                if (i % 2 == 0)
                    GUI.DrawTexture(row, _darkBackground);

                //// draw the slot - grey out if draggin this, but only when dragged over somewhere else
                //if (Dragging == _currentLoadout.Slots[i] && !Mouse.IsOver(row))
                //    GUI.color = new Color(.6f, .6f, .6f, .4f);
                DrawSlot(row, _currentLoadout.CachedList[i], reorderableGroup);
                GUI.color = Color.white;
            }

            // if we're dragging, create an extra invisible row to allow moving stuff to the bottom
            //if (Dragging != null)
            //{
            //    Rect row = new Rect(0f, curY, viewRect.width, GenUI.ListSpacing);

            //    if (Mouse.IsOver(row))
            //    {
            //        // draw ghost
            //        GUI.color = new Color(.7f, .7f, .7f, .5f);
            //        DrawSlot(row, Dragging);
            //        GUI.color = Color.white;

            //        // catch mouseUp
            //        if (Input.GetMouseButtonUp(0))
            //        {
            //            _currentLoadout.MoveSlot(Dragging, _currentLoadout.Slots.Count - 1);
            //            Dragging = null;
            //        }
            //    }
            //}

            //// cancel drag when mouse leaves the area, or on mouseup.
            //if (!Mouse.IsOver(viewRect) || Input.GetMouseButtonUp(0))
            //    Dragging = null;

            if (Event.current.type == EventType.Layout)
            {
                _scrollViewHeight = curY + GenUI.ListSpacing;
            }
            Widgets.EndScrollView();
        }

        private void DrawItemsInCategory(Rect canvas)
        {
            int count = _sourceType == SourceSelection.Generic ? _sourceGeneric.Count : _source.Count;
            GUI.DrawTexture(canvas, _darkBackground);

            if ((_sourceType != SourceSelection.Generic && _source.NullOrEmpty()) || (_sourceType == SourceSelection.Generic && _sourceGeneric.NullOrEmpty()))
                return;

            Rect viewRect = new Rect(canvas);
            viewRect.width -= 16f;
            viewRect.height = count * GenUI.ListSpacing;

            Widgets.BeginScrollView(canvas, ref _availableScrollPosition, viewRect.AtZero());
            int startRow = (int)Math.Floor((decimal)(_availableScrollPosition.y / GenUI.ListSpacing));
            startRow = (startRow < 0) ? 0 : startRow;
            int endRow = startRow + (int)(Math.Ceiling((decimal)(canvas.height / GenUI.ListSpacing)));
            endRow = (endRow > count) ? count : endRow;
            for (int i = startRow; i < endRow; i++)
            {
                // gray out weapons not in stock
                Color baseColor = GUI.color;

                if (_source[i].isGreyedOut)
                    GUI.color = Color.gray;

                Rect row = new Rect(0f, i * GenUI.ListSpacing, canvas.width, GenUI.ListSpacing);
                Rect labelRect = new Rect(row);
                if (_sourceType == SourceSelection.Generic)
                    TooltipHandler.TipRegion(row, (_sourceGeneric[i] as ThingDef).GetWeightAndBulkTip());
                else
                    TooltipHandler.TipRegion(row, _source[i].thingDef.GetWeightAndBulkTip());

                labelRect.xMin += _margin;
                if (i % 2 == 0)
                    GUI.DrawTexture(row, _darkBackground);

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;

                Widgets.Label(labelRect, _source[i].thingDef.LabelCap);
                Text.WordWrap = true;
                Text.Anchor = TextAnchor.UpperLeft;

                // Draw clickable button
                Widgets.DrawHighlightIfMouseover(row);
                if (Widgets.ButtonInvisible(row))
                {
                    _currentLoadout.AddItem(_source[i].thingDef);
                    //_currentLoadout.CachedList = _currentLoadout.ToList();
                }
                // revert to original color
                GUI.color = baseColor;
            }
            Widgets.EndScrollView();
        }

        private void ReorderItems(int oldIndex, int newIndex)
        {
            if (oldIndex != newIndex)
            {
                _currentLoadout.CachedList.Insert(newIndex, _currentLoadout.CachedList[oldIndex]);
                _currentLoadout.CachedList.RemoveAt((oldIndex >= newIndex) ? (oldIndex + 1) : oldIndex);
            }
        }

        private void DrawSourceIcon(SourceSelection sourceSelected, Texture2D texButton, ref Rect button, string tip)
        {
            GUI.color = _sourceType == sourceSelected ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, texButton))
            {
                SetSource(sourceSelected);
                _availableScrollPosition = Vector2.zero;
            }
            TooltipHandler.TipRegion(button, tip);
            button.x += 24f + _margin;
        }

        private void ExtraDraggedItemOnGUI(int index, Vector2 startPos)
        {
            Color color = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.5f);
        }

        #endregion Methods

        private class SelectableItem
        {
            public ThingDef thingDef;
            public bool isGreyedOut;
        }
    }
}