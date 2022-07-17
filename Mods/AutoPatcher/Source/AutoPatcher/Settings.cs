using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AutoPatcher
{
    public class Settings : ModSettings
    {
        public List<Node> allNodes = new List<Node>();
        public List<PatchTreeBranch> allBranches = new List<PatchTreeBranch>();
        public HashSet<string> modList;
        public Dictionary<PatchTreeDef, int> patchTreeDebug = new Dictionary<PatchTreeDef, int>();
        public Dictionary<Node, int> nodeDebug = new Dictionary<Node, int>();
        private PatchTreeDef patchTree;
        private string Filter;
        private bool flagChanged = false;
        public void DoWindowContents(Rect wrect)
        {
            Listing_Standard options = new Listing_Standard();
            Color defaultColor = GUI.color;
            options.Begin(wrect);
            Rect rect0 = new Rect(0f, 0f, wrect.width - 30f, wrect.height);

            GUI.color = defaultColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            options.Gap();
            float lineHeight = Text.LineHeight;
            Rect rect1a = options.GetRect(lineHeight);
            Rect rect1b = new Rect(rect1a.x + options.ColumnWidth - 275f, rect1a.y, 198f, 24f);
            Rect rect1c = new Rect(rect1a.x + options.ColumnWidth - 75f, rect1b.y, 73f, 24f);
            Widgets.Label(rect1a, "AP_PatchTreeDebug".Translate());
            Widgets.Dropdown(rect1b, patchTree, t => t, PatchTreeDebugElements, patchTree?.defName);
            if (patchTree != null)
            {
                var val = patchTreeDebug.TryGetValue(patchTree, int.MinValue);
                Widgets.Dropdown(rect1c, patchTree, t => patchTreeDebug.TryGetValue(t, int.MinValue), PatchTreeDebugValues, val == int.MinValue ? "Default".Translate().ToString() : val.ToString());
            }
            options.Gap();
            Filter = options.TextEntryLabeled("Filter".Translate(), Filter, 1);
#if V11
            var itemsWindow = options.BeginSection(340f);
#else
            var itemsWindow = options.BeginSection(340f);
#endif
            itemsWindow.ColumnWidth = ((rect0.width - 50f) / 3f);
            if (patchTree != null)
                foreach (var node in patchTree?.Nodes)
                {
                    string text = node.ToString();
                    if (text.ToLower().Contains(Filter?.ToLower()))
                    {
                        Rect rect3 = itemsWindow.GetRect(lineHeight);
                        TextAnchor anchor = Text.Anchor;
                        // Text.Anchor = TextAnchor.MiddleLeft;
                        var val = nodeDebug.TryGetValue(node, int.MinValue);
                        var valText = val == int.MinValue ? "Default".Translate().ToString() : val.ToString();
                        Widgets.Label(rect3, text);
                        Rect rect4 = new Rect(rect3.x + itemsWindow.ColumnWidth - 75f, rect3.y, 73f, 24f);
                        Widgets.Dropdown(rect4, node, t => nodeDebug.TryGetValue(t, int.MinValue), NodeDebugValues, valText, paintable: true);
                    }
                }
            itemsWindow.End();
            options.End();
            if (flagChanged)
            {
                Mod.GetSettings<Settings>().Write();
                flagChanged = false;
            }
        }
        private IEnumerable<Widgets.DropdownMenuElement<PatchTreeDef>> PatchTreeDebugElements(PatchTreeDef _)
        {
            foreach (var patchTree in DefDatabase<PatchTreeDef>.AllDefs)
                yield return new Widgets.DropdownMenuElement<PatchTreeDef>
                {
                    option = new FloatMenuOption(patchTree.defName, () => this.patchTree = patchTree),
                    payload = patchTree,
                };
        }
        private IEnumerable<Widgets.DropdownMenuElement<int>> PatchTreeDebugValues(PatchTreeDef patchTree)
        {
            yield return new Widgets.DropdownMenuElement<int>
            {
                option = new FloatMenuOption("Default".Translate(), () =>
                {
                    flagChanged = true;
                    if (patchTreeDebug.ContainsKey(patchTree))
                        patchTreeDebug.Remove(patchTree);
                }),
                payload = int.MinValue,
            };
            for (int i = -2; i < 11; i++)
            {
                var j = i;
                yield return new Widgets.DropdownMenuElement<int>
                {
                    option = new FloatMenuOption(j.ToString(), () => {
                        flagChanged = true;
                        patchTreeDebug.SetOrAdd(patchTree, j);
                    }),
                    payload = j,
                };
            }
        }
        private IEnumerable<Widgets.DropdownMenuElement<int>> NodeDebugValues(Node node)
        {
            yield return new Widgets.DropdownMenuElement<int>
            {
                option = new FloatMenuOption("Default".Translate(), () =>
                {
                    flagChanged = true;
                    if (nodeDebug.ContainsKey(node))
                        nodeDebug.Remove(node);
                }),
                payload = int.MinValue,
            };
            for (int i = -2; i < 11; i++)
            {
                var j = i;
                yield return new Widgets.DropdownMenuElement<int>
                {
                    option = new FloatMenuOption(j.ToString(), () => {
                        flagChanged = true;
                        nodeDebug.SetOrAdd(node, j);
                    }),
                    payload = j,
                };
            }
        }
        private List<Node> nodeDebugKeys = new List<Node>();
        private List<int> nodeDebugValues = new List<int>();
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                allNodes = DefDatabase<PatchTreeDef>.AllDefs.SelectMany(t => t.Nodes).ToList();
                allBranches = DefDatabase<PatchTreeDef>.AllDefs.SelectMany(t => t.Branches).ToList();
            }
            Scribe_Collections.Look(ref allNodes, "allNodes", LookMode.Deep);
            Scribe_Collections.Look(ref allBranches, "allBranches", LookMode.Deep);
            Scribe_Collections.Look(ref modList, "modList");
            Scribe_Collections.Look(ref patchTreeDebug, "patchTreeDebug", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref nodeDebug, "nodeDebug", LookMode.Reference, LookMode.Value, ref nodeDebugKeys, ref nodeDebugValues);
        }
    }
}
