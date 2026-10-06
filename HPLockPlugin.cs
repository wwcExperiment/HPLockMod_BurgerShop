using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace HPLockMod
{
    /// <summary>
    /// 锁血 Mod：按 [1]（主键盘或小键盘）切换锁定 HP；锁住时血条变金色。
    ///
    /// 实现方式：不 patch 任何方法（本游戏的元数据会让 Harmony/MonoMod 的
    /// 运行时改写崩溃），而是在每一帧直接改 PlayerControl 的公开字段
    /// maxHP / currentHP，把 HP 顶到 999999，从根上杜绝扣血和 GameOver。
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class HPLockModPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.fnbs.hplock";
        public const string NAME = "HP Lock 锁血";
        public const string VERSION = "1.0.0";

        private const float LOCK_HP = 999999f;

        // 锁住时血条的颜色（金色）。想换颜色改这里，例如青色 new Color(0.2f, 1f, 1f, 1f)。
        private static readonly Color LockColor = new Color(1f, 0.8f, 0.2f, 1f);

        private bool locked = false;
        private bool wasLockedLastFrame = false;
        private float savedMaxHP = 100f;

        private UIControl uiControl;
        private Color originalCurrent = Color.white;
        private Color originalDisplay = Color.white;
        private bool colorsCaptured = false;

        private void Update()
        {
            // 1) 切换开关
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                locked = !locked;
                Logger.LogInfo(locked ? "[锁血] 已锁定 HP" : "[锁血] 已解锁 HP");
            }

            // 2) 每帧把 HP 顶满（锁定）/ 恢复（解锁）
            var player = FindObjectOfType<PlayerControl>();
            if (player != null)
            {
                if (locked)
                {
                    if (!wasLockedLastFrame)
                    {
                        savedMaxHP = player.maxHP; // 刚锁定：快照原始上限
                    }
                    player.maxHP = LOCK_HP;
                    player.currentHP = LOCK_HP;
                }
                else if (wasLockedLastFrame)
                {
                    player.maxHP = savedMaxHP; // 刚解锁：恢复上限与满血
                    player.currentHP = savedMaxHP;
                }
                wasLockedLastFrame = locked;
            }

            // 3) 血条颜色
            if (uiControl == null)
            {
                uiControl = FindObjectOfType<UIControl>();
                if (uiControl != null) colorsCaptured = false;
            }
            if (uiControl == null) return;

            if (!colorsCaptured)
            {
                if (uiControl.currentHPGauge != null) originalCurrent = uiControl.currentHPGauge.color;
                if (uiControl.displayHPGauge != null) originalDisplay = uiControl.displayHPGauge.color;
                colorsCaptured = true;
            }

            if (uiControl.currentHPGauge != null)
                uiControl.currentHPGauge.color = locked ? LockColor : originalCurrent;
            if (uiControl.displayHPGauge != null)
                uiControl.displayHPGauge.color = locked ? LockColor : originalDisplay;
        }
    }
}
