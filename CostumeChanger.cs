using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace HPLockMod
{
    /// <summary>
    /// 衣着 Mod：按 [5] 呼出/关闭一个独立的半透明小面板，用游戏自带图标控制坐姿女孩的衣着。
    ///
    /// 交互：按 5 只开/关面板，不改任何东西；点某个图标才改，且下一帧立即生效。
    /// 图标：启动时从 CustomUIManager 的 15 个衣着按钮里读取 Image.sprite（只读引用，不改场景）。
    /// 原理：衣着由 TabemiControl 的公开字段 Cap/Upper/Lower/Tights/Glasses 决定，
    /// TabemiControl.Update() 每帧调用 UpdateCostume() 写进 Live2D 参数，直接改字段即生效。
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CostumeChangerPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.fnbs.costumechanger";
        public const string NAME = "Costume Changer 衣着";
        public const string VERSION = "1.3.0";

        private const KeyCode ToggleKey1 = KeyCode.Alpha5;   // 主键盘 5
        private const KeyCode ToggleKey2 = KeyCode.Keypad5;  // 小键盘 5

        private bool panelVisible = false;
        private TabemiControl tabemi;

        // ===== 面板外观（想换位置/颜色改这里） =====
        private const float PanelX = 10f;   // 面板左上角 X
        private const float PanelY = 90f;   // 面板左上角 Y（往下挪了）
        private const float PanelW = 320f;  // 面板宽
        private const float TitleH = 26f;   // 标题行高
        private const float RowH = 46f;     // 每行高（加高了）
        private const float CellW = 40f;    // 图标单元格宽
        private const float CellH = 40f;    // 图标单元格高
        private const float CellGap = 6f;   // 单元格间距
        private const float LabelW = 60f;   // 分类名宽度
        private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.62f);            // 面板底色（半透明）
        private static readonly Color DimColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);      // 未选中：变暗半透明（与原面板一致）
        // ============================================

        // 分类名；数组下标即分类（0=帽子 1=上衣 2=下半身 3=丝袜 4=眼镜）。
        private static readonly string[] CatNames = { "帽子", "上衣", "下半身", "丝袜", "眼镜" };

        // 图标取不到的兜底文字
        private static readonly string[][] FallbackLabels =
        {
            new[] { "帽0", "帽1" },
            new[] { "衣0", "衣1" },
            new[] { "无", "裙A", "裙B", "裙C", "热裤" },
            new[] { "无", "黑", "白" },
            new[] { "无", "镜A", "镜B" },
        };

        // (序列化字段名, 分类索引)
        private static readonly (string fieldName, int cat)[] ClothingButtonDefs =
        {
            ("頭0", 0), ("頭1", 0),
            ("上半身0", 1), ("上半身1", 1),
            ("下半身0", 2), ("下半身1", 2), ("下半身2", 2), ("下半身3", 2), ("下半身4", 2),
            ("タイツ0", 3), ("タイツ1", 3), ("タイツ2", 3),
            ("眼鏡0", 4), ("眼鏡1", 4), ("眼鏡2", 4),
        };

        private bool iconsLoaded = false;
        private Sprite[][] icons; // [分类][选项]

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey1) || Input.GetKeyDown(ToggleKey2))
            {
                panelVisible = !panelVisible;
                Logger.LogInfo(panelVisible ? "[衣着] 面板开启" : "[衣着] 面板关闭");
            }
        }

        private void OnGUI()
        {
            if (!panelVisible) return;

            if (tabemi == null)
            {
                tabemi = FindObjectOfType<TabemiControl>();
                if (tabemi == null) return;
            }
            EnsureIcons();

            float panelH = TitleH + RowH * CatNames.Length + 10f;

            GUI.backgroundColor = PanelBg;
            GUI.Box(new Rect(PanelX, PanelY, PanelW, panelH), GUIContent.none);
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(PanelX + 12f, PanelY + 5f, PanelW - 24f, 20f), "衣着（按 5 关闭）");

            float y = PanelY + TitleH + 2f;
            for (int cat = 0; cat < CatNames.Length; cat++)
            {
                GUI.Label(new Rect(PanelX + 12f, y + (RowH - 20f) / 2f, LabelW, 20f), CatNames[cat]);

                int current = GetValue(cat);
                float x = PanelX + 12f + LabelW + 4f;
                for (int opt = 0; opt < FallbackLabels[cat].Length; opt++)
                {
                    Rect cell = new Rect(x, y, CellW, CellH);
                    DrawCell(cell, cat, opt, current);
                    x += CellW + CellGap;
                }
                y += RowH;
            }
        }

        private void DrawCell(Rect cell, int cat, int opt, int current)
        {
            bool selected = opt == current;

            // 原版选中效果：选中 = 正常亮，未选中 = 变暗半透明
            GUI.color = selected ? Color.white : DimColor;

            Sprite s = GetIcon(cat, opt);
            if (s != null)
            {
                DrawSprite(new Rect(cell.x + 2f, cell.y + 2f, cell.width - 4f, cell.height - 4f), s);
            }
            else
            {
                GUI.Label(cell, FallbackLabels[cat][opt]);
            }
            GUI.color = Color.white;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                cell.Contains(Event.current.mousePosition))
            {
                SetValue(cat, opt);
                Event.current.Use();
            }
        }

        // 启动时从 CustomUIManager 读取 15 个衣着按钮的 sprite（只读，不改场景）。
        private void EnsureIcons()
        {
            if (iconsLoaded) return;
            iconsLoaded = true;

            icons = new Sprite[CatNames.Length][];
            CustomUIManager ui = FindObjectOfType<CustomUIManager>(includeInactive: true);
            if (ui == null)
            {
                Logger.LogWarning("[衣着] 未找到 CustomUIManager，图标将回退为文字");
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (var (fieldName, cat) in ClothingButtonDefs)
            {
                FieldInfo field = typeof(CustomUIManager).GetField(fieldName, flags);
                if (field == null) continue;
                Button btn = field.GetValue(ui) as Button;
                if (btn == null) continue;

                int opt = fieldName[fieldName.Length - 1] - '0';
                if (icons[cat] == null) icons[cat] = new Sprite[FallbackLabels[cat].Length];
                icons[cat][opt] = (btn.image != null) ? btn.image.sprite : null;
            }
        }

        private Sprite GetIcon(int cat, int opt)
        {
            if (icons == null || icons[cat] == null || opt >= icons[cat].Length) return null;
            return icons[cat][opt];
        }

        // 在给定矩形内按比例居中绘制 Sprite（支持图集）。
        private static void DrawSprite(Rect position, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            if (tex == null) return;

            Vector2 fullSize = new Vector2(tex.width, tex.height);
            Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

            Rect coords = sprite.textureRect;
            coords.x /= fullSize.x;
            coords.width /= fullSize.x;
            coords.y /= fullSize.y;
            coords.height /= fullSize.y;

            Vector2 ratio = new Vector2(position.width / size.x, position.height / size.y);
            float minRatio = Mathf.Min(ratio.x, ratio.y);

            Vector2 center = position.center;
            position.size = size * minRatio;
            position.center = center;

            GUI.DrawTextureWithTexCoords(position, tex, coords);
        }

        private int GetValue(int idx)
        {
            switch (idx)
            {
                case 0: return tabemi.Cap;
                case 1: return tabemi.Upper;
                case 2: return tabemi.Lower;
                case 3: return tabemi.Tights;
                default: return tabemi.Glasses;
            }
        }

        private void SetValue(int idx, int v)
        {
            switch (idx)
            {
                case 0: tabemi.Cap = v; break;
                case 1: tabemi.Upper = v; break;
                case 2: tabemi.Lower = v; break;
                case 3: tabemi.Tights = v; break;
                default: tabemi.Glasses = v; break;
            }
        }
    }
}
