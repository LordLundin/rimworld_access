using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RimWorldAccess
{
    /// <summary>
    /// Defines the types of zones that can be created.
    /// </summary>
    public enum ZoneType
    {
        Stockpile,
        DumpingStockpile,
        GrowingZone
    }

    /// <summary>
    /// Maintains state for zone creation mode.
    /// Tracks which cells have been selected and what type of zone to create.
    /// </summary>
    public static class ZoneCreationState
    {
        private static bool isInCreationMode = false;
        private static ZoneType selectedZoneType = ZoneType.Stockpile;
        private static List<IntVec3> selectedCells = new List<IntVec3>();

        /// <summary>
        /// Whether zone creation mode is currently active.
        /// </summary>
        public static bool IsInCreationMode
        {
            get => isInCreationMode;
            private set => isInCreationMode = value;
        }

        /// <summary>
        /// The type of zone being created.
        /// </summary>
        public static ZoneType SelectedZoneType
        {
            get => selectedZoneType;
            private set => selectedZoneType = value;
        }

        /// <summary>
        /// List of cells that have been selected for the zone.
        /// </summary>
        public static List<IntVec3> SelectedCells => selectedCells;

        /// <summary>
        /// Enters zone creation mode with the specified zone type.
        /// </summary>
        public static void EnterCreationMode(ZoneType zoneType)
        {
            isInCreationMode = true;
            selectedZoneType = zoneType;
            selectedCells.Clear();
            
            string zoneName = GetZoneTypeName(zoneType);
            ClipboardHelper.CopyToClipboard($"Creating {zoneName}. Press Space to select tiles, Enter to confirm, Escape to cancel");
            MelonLoader.MelonLogger.Msg($"Entered zone creation mode: {zoneName}");
        }

        /// <summary>
        /// Adds a cell to the selection if not already selected.
        /// </summary>
        public static void AddCell(IntVec3 cell)
        {
            if (!selectedCells.Contains(cell))
            {
                selectedCells.Add(cell);
                ClipboardHelper.CopyToClipboard($"Selected, {cell.x}, {cell.z}");
            }
            else
            {
                ClipboardHelper.CopyToClipboard($"Already selected, {cell.x}, {cell.z}");
            }
        }

        /// <summary>
        /// Removes a cell from the selection.
        /// </summary>
        public static void RemoveCell(IntVec3 cell)
        {
            if (selectedCells.Remove(cell))
            {
                ClipboardHelper.CopyToClipboard($"Deselected, {cell.x}, {cell.z}");
            }
        }

        /// <summary>
        /// Checks if a cell is currently selected.
        /// </summary>
        public static bool IsCellSelected(IntVec3 cell)
        {
            return selectedCells.Contains(cell);
        }

        /// <summary>
        /// Creates the zone with all selected cells and exits creation mode.
        /// </summary>
        public static void CreateZone(Map map)
        {
            if (selectedCells.Count == 0)
            {
                ClipboardHelper.CopyToClipboard("No cells selected. Zone not created.");
                Cancel();
                return;
            }

            Zone newZone = null;
            string zoneName = "";

            try
            {
                switch (selectedZoneType)
                {
                    case ZoneType.Stockpile:
                        Zone_Stockpile stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
                        newZone = stockpile;
                        zoneName = "Stockpile zone";
                        break;

                    case ZoneType.DumpingStockpile:
                        Zone_Stockpile dumpingStockpile = new Zone_Stockpile(StorageSettingsPreset.DumpingStockpile, map.zoneManager);
                        newZone = dumpingStockpile;
                        zoneName = "Dumping stockpile zone";
                        break;

                    case ZoneType.GrowingZone:
                        Zone_Growing growingZone = new Zone_Growing(map.zoneManager);
                        newZone = growingZone;
                        zoneName = "Growing zone";
                        break;
                }

                if (newZone != null)
                {
                    // Register the zone
                    map.zoneManager.RegisterZone(newZone);

                    // Add all selected cells
                    foreach (IntVec3 cell in selectedCells)
                    {
                        if (cell.InBounds(map))
                        {
                            newZone.AddCell(cell);
                        }
                    }

                    ClipboardHelper.CopyToClipboard($"{zoneName} created with {selectedCells.Count} cells");
                    MelonLoader.MelonLogger.Msg($"Created {zoneName} with {selectedCells.Count} cells");
                }
                else
                {
                    ClipboardHelper.CopyToClipboard("Failed to create zone");
                    MelonLoader.MelonLogger.Error("Failed to create zone: newZone was null");
                }
            }
            catch (System.Exception ex)
            {
                ClipboardHelper.CopyToClipboard($"Error creating zone: {ex.Message}");
                MelonLoader.MelonLogger.Error($"Error creating zone: {ex}");
            }
            finally
            {
                Reset();
            }
        }

        /// <summary>
        /// Cancels zone creation and exits creation mode.
        /// </summary>
        public static void Cancel()
        {
            ClipboardHelper.CopyToClipboard("Zone creation cancelled");
            MelonLoader.MelonLogger.Msg("Zone creation cancelled");
            Reset();
        }

        /// <summary>
        /// Resets the state, exiting creation mode.
        /// </summary>
        public static void Reset()
        {
            isInCreationMode = false;
            selectedCells.Clear();
        }

        /// <summary>
        /// Gets a human-readable name for a zone type.
        /// </summary>
        private static string GetZoneTypeName(ZoneType type)
        {
            switch (type)
            {
                case ZoneType.Stockpile:
                    return "stockpile zone";
                case ZoneType.DumpingStockpile:
                    return "dumping stockpile zone";
                case ZoneType.GrowingZone:
                    return "growing zone";
                default:
                    return "zone";
            }
        }
    }
}
