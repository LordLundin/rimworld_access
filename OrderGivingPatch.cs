using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess
{
    /// <summary>
    /// Harmony patch for UIRoot.UIRootOnGUI to add accessible order-giving with the Enter key.
    /// When a pawn is selected and Enter is pressed, shows a menu of interaction options
    /// for objects at the current map cursor position.
    /// Implements a two-stage flow: first select target (if multiple), then select action.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot))]
    [HarmonyPatch("UIRootOnGUI")]
    public static class OrderGivingPatch
    {
        /// <summary>
        /// Prefix patch that intercepts Enter key to trigger order-giving mode.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix()
        {
            // Only process keyboard events
            if (Event.current.type != EventType.KeyDown)
                return;

            KeyCode key = Event.current.keyCode;

            // If windowless menu is active, handle navigation keys
            if (WindowlessFloatMenuState.IsActive)
            {
                bool handled = false;

                if (key == KeyCode.DownArrow)
                {
                    WindowlessFloatMenuState.SelectNext();
                    handled = true;
                }
                else if (key == KeyCode.UpArrow)
                {
                    WindowlessFloatMenuState.SelectPrevious();
                    handled = true;
                }
                else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
                {
                    WindowlessFloatMenuState.ExecuteSelected();
                    handled = true;
                }
                else if (key == KeyCode.Escape)
                {
                    WindowlessFloatMenuState.Close();
                    ClipboardHelper.CopyToClipboard("Menu closed");
                    handled = true;
                }

                if (handled)
                {
                    Event.current.Use();
                }
                return;
            }


            // Don't process if in zone creation mode
            if (ZoneCreationState.IsInCreationMode)
                return;
            // Only process Enter key for opening menu
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter)
                return;

            // Only process during normal gameplay with a valid map
            if (Find.CurrentMap == null)
                return;

            // Don't process if any dialog or window that prevents camera motion is open
            if (Find.WindowStack != null && Find.WindowStack.WindowsPreventCameraMotion)
                return;

            // Check if map navigation is initialized
            if (!MapNavigationState.IsInitialized)
                return;

            // Check if any pawns are selected
            if (Find.Selector == null || !Find.Selector.SelectedPawns.Any())
            {
                ClipboardHelper.CopyToClipboard("No pawn selected");
                Event.current.Use();
                return;
            }

            // Get the cursor position
            IntVec3 cursorPosition = MapNavigationState.CurrentCursorPosition;
            Map map = Find.CurrentMap;

            // Validate cursor position
            if (!cursorPosition.IsValid || !cursorPosition.InBounds(map))
            {
                ClipboardHelper.CopyToClipboard("Invalid position");
                Event.current.Use();
                return;
            }


            // Get selected pawns
            List<Pawn> selectedPawns = Find.Selector.SelectedPawns.ToList();

            // Get all available actions for this position using RimWorld's built-in system
            Vector3 clickPos = cursorPosition.ToVector3Shifted();
            List<FloatMenuOption> options = FloatMenuMakerMap.GetOptions(
                selectedPawns,
                clickPos,
                out FloatMenuContext context
            );

            if (options != null && options.Count > 0)
            {
                // Open the windowless menu with these options
                WindowlessFloatMenuState.Open(options, true); // true = gives colonist orders
            }
            else
            {
                ClipboardHelper.CopyToClipboard("No available actions");
            }

            // Consume the event
            Event.current.Use();
        }

}
}
