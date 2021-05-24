using Script.Controller.Bullet;
using Script.JudgeSystem.Event;
using UnityEngine.UIElements;

namespace Script.JudgeSystem
{
    /*
     * 可伤害对象的控制器应实现此接口（未详细设计）
     * 可伤害对象包括机器人和建筑，Hurt函数应由控制器实现，由装甲板调用
     */
    public interface IVulnerable
    {
        void Hit(int hitter, CaliberT caliber, bool isTriangle = false);
    }
}