using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ImprovedBeltBag.Patches
{
    /// <summary>
    /// Rebuilds the belt bag inventory grid so the number of visible slot squares matches
    /// <see cref="PluginConfig.Capacity"/>.
    ///
    /// Vanilla <c>BeltBagInventoryUI.FillSlots</c> loops over <c>inventorySlots.Length</c> and sets
    /// <c>inventorySlotIcons[i]</c>, so growing/shrinking those two arrays (and cloning/hiding the
    /// matching slot GameObjects) makes the icons display at any count. To keep the vanilla look we
    /// read the original grid's cell spacing and centre from the pristine slots, then lay the new
    /// slot count out manually — centred in the window with the same gaps — instead of letting a
    /// layout group cram them into a corner. Everything is guarded; if it misbehaves in your build,
    /// set [Slots] Resize Inventory UI = false and the functional capacity still works.
    /// </summary>
    [HarmonyPatch]
    internal static class BagUiPatch
    {
        private static string _iconPath;

        // Captured once from the original (vanilla) grid.
        private static bool _captured;
        private static Vector2 _cellSize;
        private static float _xStep, _yStep;
        private static Vector2 _center;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BeltBagInventoryUI), nameof(BeltBagInventoryUI.FillSlots))]
        private static void BeforeFillSlots(BeltBagInventoryUI __instance)
        {
            if (!BeltBagPatch.Enabled || !PluginConfig.Enabled.Value || !PluginConfig.ResizeSlotUI.Value)
                return;

            try { EnsureSlots(__instance); }
            catch (System.Exception e) { Plugin.Log.LogError($"Belt bag UI resize failed (harmless): {e}"); }
        }

        private static void EnsureSlots(BeltBagInventoryUI ui)
        {
            var slots = ui.inventorySlots;
            var icons = ui.inventorySlotIcons;
            if (slots == null || slots.Length == 0 || icons == null || icons.Length == 0) return;

            int target = Mathf.Clamp(PluginConfig.Capacity.Value, 1, 100);
            int current = slots.Length;
            if (current == target) return; // already sized (positions persist)

            var template = slots[0];
            var container = template.transform.parent;
            if (_iconPath == null) _iconPath = RelativePath(slots[0].transform, icons[0].transform);

            // Read the vanilla grid metrics before we touch anything.
            CaptureMetrics(slots);
            DisableLayoutGroups(container);

            var newSlots = new GameObject[target];
            var newIcons = new Image[target];

            for (int i = 0; i < target; i++)
            {
                GameObject slot;
                Image icon;
                if (i < current && slots[i] != null)
                {
                    slot = slots[i];
                    icon = icons[i];
                }
                else
                {
                    slot = Object.Instantiate(template, container);
                    slot.name = $"BeltBagSlot({i})";
                    icon = ResolveIcon(slot);
                    RewireClick(ui, slot, i);
                }

                slot.SetActive(true);
                if (icon != null) icon.enabled = false;
                newSlots[i] = slot;
                newIcons[i] = icon;
            }

            for (int i = target; i < current; i++)
                if (slots[i] != null) slots[i].SetActive(false);

            ui.inventorySlots = newSlots;
            ui.inventorySlotIcons = newIcons;

            LayoutSlots(newSlots);
        }

        // -------- vanilla grid metrics --------

        private static void CaptureMetrics(GameObject[] slots)
        {
            if (_captured) return;

            var rt0 = slots[0].GetComponent<RectTransform>();
            _cellSize = rt0.rect.size;
            float y0 = rt0.anchoredPosition.y;

            int perRow = 0;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            RectTransform secondCol = null, secondRow = null;

            foreach (var s in slots)
            {
                var rt = s.GetComponent<RectTransform>();
                var p = rt.anchoredPosition;
                if (Mathf.Abs(p.y - y0) < 1f) { perRow++; if (secondCol == null && p.x > rt0.anchoredPosition.x + 1f) secondCol = rt; }
                else if (secondRow == null) secondRow = rt;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }

            _xStep = secondCol != null
                ? Mathf.Abs(secondCol.anchoredPosition.x - rt0.anchoredPosition.x)
                : Mathf.Max(1f, _cellSize.x * 1.15f);
            _yStep = secondRow != null
                ? Mathf.Abs(secondRow.anchoredPosition.y - y0)
                : Mathf.Max(1f, _cellSize.y * 1.15f);
            _center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            _captured = true;
        }

        private static void LayoutSlots(GameObject[] slots)
        {
            if (!_captured) return;
            int n = slots.Length;
            int perRow = Mathf.Max(1, PluginConfig.SlotsPerRow.Value);
            int rows = Mathf.CeilToInt(n / (float)perRow);
            float totalH = (rows - 1) * _yStep;

            for (int i = 0; i < n; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                int itemsInRow = Mathf.Min(perRow, n - row * perRow);
                float rowW = (itemsInRow - 1) * _xStep;

                float x = _center.x - rowW * 0.5f + col * _xStep;
                float y = _center.y + totalH * 0.5f - row * _yStep;

                var rt = slots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(x, y);
                    if (_cellSize.sqrMagnitude > 0.01f) rt.sizeDelta = _cellSize;
                }
            }
        }

        private static void DisableLayoutGroups(Transform container)
        {
            foreach (var lg in container.GetComponents<LayoutGroup>()) lg.enabled = false;
            foreach (var csf in container.GetComponents<ContentSizeFitter>()) csf.enabled = false;
        }

        // -------- helpers --------

        private static Image ResolveIcon(GameObject slotClone)
        {
            if (string.IsNullOrEmpty(_iconPath))
                return slotClone.GetComponent<Image>();
            var t = slotClone.transform.Find(_iconPath);
            return t != null ? t.GetComponent<Image>() : slotClone.GetComponentInChildren<Image>(true);
        }

        private static void RewireClick(BeltBagInventoryUI ui, GameObject slot, int index)
        {
            var button = slot.GetComponentInChildren<Button>(true);
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ui.ClickInventorySlot(index));
        }

        private static string RelativePath(Transform root, Transform child)
        {
            if (child == root || child == null) return "";
            var sb = new StringBuilder();
            var t = child;
            while (t != null && t != root)
            {
                if (sb.Length > 0) sb.Insert(0, '/');
                sb.Insert(0, t.name);
                t = t.parent;
            }
            return sb.ToString();
        }
    }
}
