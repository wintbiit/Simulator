using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private int _slowDecisionUpdate;
        private Decision _decider;

        private void Start()
        {
            new Thread(StartDecisionSystem).Start();
        }

        private void StartDecisionSystem()
        {
#if UNITY_EDITOR
            var curDir = Environment.CurrentDirectory + "\\Client\\Decision";
            var exeFile = curDir + "\\SD.exe";
#else
            var curDir = Environment.CurrentDirectory + "\\..\\Decision";
            var exeFile = curDir + "\\SD.exe";
#endif
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
            _decider = new Decision();
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
            if (_decider == null) return;
            var em = (EnergyMechanismController) Gm.clientFacilityBases
                .FindAll(f => f.role.Type == TypeT.EnergyMechanism).First();
            _decider.Decide(new Situation
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
            if (_decider.Code == -1) return;
            var m = StrategyTable.Table[_decider.Code].Messages;
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