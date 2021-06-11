using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Mirror;
using Script.Controller;
using Script.Controller.Armor;
using Script.JudgeSystem;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using TMPro;

namespace Script.UI.HUD
{
    public class StrategyUI : HUDBase
    {
        public TMP_Text label;
        public TMP_Text strategyDisplay;
        public Decision Decider;
        private int _slowDecisionUpdate;

        private void Start()
        {
            if (NetworkClient.active)
                new Thread(StartDecisionSystem).Start();
        }

        private void StartDecisionSystem()
        {
#if UNITY_EDITOR
            var curDir = Environment.CurrentDirectory + "\\Client\\Decision";
#else
            var curDir = Environment.CurrentDirectory + "\\..\\Decision";
#endif
            var exeFile = curDir + "\\SD.exe";
            var process = new Process
            {
                StartInfo =
                {
                    FileName = exeFile,
                    WorkingDirectory = curDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start()) return;
            Decider = new Decision();
            process.WaitForExit();
        }


        public override void Refresh(RobotBase localRobot)
        {
            label.enabled = true;
            strategyDisplay.enabled = true;
            // 决策
            _slowDecisionUpdate++;
            if (_slowDecisionUpdate <= 20) return;
            _slowDecisionUpdate = 0;
            if (Decider == null) return;
            var em = (EnergyMechanismController) Gm.clientFacilityBases
                .FindAll(f => f.role.Type == TypeT.EnergyMechanism).First();
            Decider.Decide(new Situation
            {
                AHP = 100,
                BuffAvailable = em.branches[0].armor.GetColor() == ColorT.Down ? 0 : 1,
                FHP = Gm.clientFacilityBases.First(f =>
                    f.role.Equals(new RoleT(localRobot.role.Camp, TypeT.Outpost))).health,
                inInvasion = 0,
                RemainTime = Gm.globalStatus.countDown,
                SHP = Gm.clientRobotBases.First(r =>
                    r.role.Equals(new RoleT(localRobot.role.Camp, TypeT.Guard))).health
            });
            if (Decider.Code == -1) return;
            var m = StrategyTable.Table[Decider.Code].Messages;
            if (m.ContainsKey(TypeT.InfantryA) && localRobot.role.IsInfantry())
                strategyDisplay.text = m[TypeT.InfantryA];
            if (m.ContainsKey(localRobot.role.Type))
                strategyDisplay.text = m[localRobot.role.Type];
        }

        protected override void Clear()
        {
            label.enabled = false;
            strategyDisplay.enabled = false;
        }
    }
}