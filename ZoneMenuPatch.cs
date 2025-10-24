using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimWorldAccess
{
    /// <summary>
    /// Harmony patch to add Z key for zone creation menu or zone settings access.
    /// Works at the GUI event level to properly integrate with WindowlessFloatMenuState.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot))]
    [HarmonyPatch("UIRootOnGUI")]
    public static class ZoneMenuPatch
    {
        private static float lastZoneKeyTime = 0f;
        private const float ZoneKeyCooldown = 0.3f;
        
        // Flag to indicate Z was handled this frame (prevents map search from opening)
        public static bool ZKeyHandledThisFrame = false;

        /// <summary>
        /// Prefix patch to check for Z key press at GUI event level.
        /// Runs before OrderGivingPatch to handle Z key.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)] // Run before OrderGivingPatch
        public static void Prefix()
        {
            // Reset the flag at the start of each GUI frame
            if (Event.current.type == EventType.Layout)
            {
                ZKeyHandledThisFrame = false;
            }

            // Only process keyboard events
            if (Event.current.type != EventType.KeyDown)
                return;

            KeyCode key = Event.current.keyCode;

            // Only process Z key
            if (key != KeyCode.Z)
                return;

            // Cooldown to prevent accidental double-presses
            if (Time.time - lastZoneKeyTime < ZoneKeyCooldown)
                return;

            lastZoneKeyTime = Time.time;

            // Only process during normal gameplay with a valid map
            if (Find.CurrentMap == null || !MapNavigationState.IsInitialized)
                return;

            // Don't process if any dialog or window that prevents camera motion is open
            if (Find.WindowStack != null && Find.WindowStack.WindowsPreventCameraMotion)
                return;

            // Don't process if windowless orders menu is active
            if (WindowlessFloatMenuState.IsActive)
                return;

            // If already in zone creation mode, don't allow opening menu/settings
            if (ZoneCreationState.IsInCreationMode)
            {
                ClipboardHelper.CopyToClipboard("Already creating a zone. Press Enter to confirm or Escape to cancel");
                Event.current.Use();
                ZKeyHandledThisFrame = true;
                return;
            }

            // Get current cursor position
            IntVec3 position = MapNavigationState.CurrentCursorPosition;
            Map map = Find.CurrentMap;

            // Check if cursor is on a zone
            Zone zone = position.GetZone(map);

            if (zone != null)
            {
                // Open zone settings
                OpenZoneSettings(zone);
            }
            else
            {
                // Show zone creation menu
                ShowZoneCreationMenu();
            }

            // Consume the event and set flag
            Event.current.Use();
            ZKeyHandledThisFrame = true;
        }

        /// <summary>
        /// Opens the settings/inspect tab for a zone.
        /// </summary>
        private static void OpenZoneSettings(Zone zone)
        {
            // Select the zone - this opens the inspect panel with zone settings
            Find.Selector.ClearSelection();
            Find.Selector.Select(zone);

            // Ensure the inspect tab is open
            if (Find.MainTabsRoot != null)
            {
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect, playSound: false);
            }

            ClipboardHelper.CopyToClipboard($"Opening settings for {zone.label}");
            MelonLoader.MelonLogger.Msg($"Opened settings for zone: {zone.label}");
        }

        /// <summary>
        /// Shows a menu for selecting which type of zone to create.
        /// Uses the WindowlessFloatMenuState for accessibility.
        /// </summary>
        private static void ShowZoneCreationMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // Stockpile zone option
            options.Add(new FloatMenuOption("Stockpile zone", () =>
            {
                ZoneCreationState.EnterCreationMode(ZoneType.Stockpile);
            }));

            // Dumping stockpile zone option
            options.Add(new FloatMenuOption("Dumping stockpile zone", () =>
            {
                ZoneCreationState.EnterCreationMode(ZoneType.DumpingStockpile);
            }));

            // Growing zone option
            options.Add(new FloatMenuOption("Growing zone", () =>
            {
                ZoneCreationState.EnterCreationMode(ZoneType.GrowingZone);
            }));

            // Open the windowless menu
            WindowlessFloatMenuState.Open(options, false); // false = doesn't give colonist orders
            
            MelonLoader.MelonLogger.Msg("Opened zone creation menu");
        }
    }

    /// <summary>
    /// Harmony patch to prevent the map search window from opening when Z is used for zone menu.
    /// </summary>
    [HarmonyPatch(typeof(PlaySettings))]
    [HarmonyPatch("DoMapControls")]
    public static class PreventMapSearchPatch
    {
        /// <summary>
        /// Prefix to check if Z key was handled by zone menu system.
        /// Returns true to continue normal processing, false to skip map search.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // If ZoneMenuPatch handled the Z key this frame, skip map search
            if (ZoneMenuPatch.ZKeyHandledThisFrame)
            {
                return false; // Skip DoMapControls - don't open map search
            }

            return true; // Continue with normal processing
        }
    }
}
