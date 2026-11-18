using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-12: builds the one Animator Controller all rigged battle models share (they are all Humanoid, D-043):
    /// states Idle, Walk, Attack, Hit, Death; parameters <c>speed</c> and <c>attackSpeed</c> (floats) and
    /// <c>attack</c>, <c>hit</c>, <c>die</c> (triggers). The clips come from <c>Art/Animations/Clips_Knight.fbx</c>.
    /// There is no real idle clip yet: Idle shows the Walking clip frozen (speed 0), see docs/cards/PB-12.md.
    /// Menu: Puzzle Kingdoms → Build Battle Animator. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.BattleAnimatorBuilder.Build
    /// </summary>
    public static class BattleAnimatorBuilder
    {
        public const string ControllerPath = "Assets/_Project/Art/Animations/BattleUnit.controller";
        private const string ClipsPath = "Assets/_Project/Art/Animations/Clips_Knight.fbx";

        [MenuItem("Puzzle Kingdoms/Build Battle Animator")]
        public static AnimatorController Build()
        {
            AnimationClip Clip(string name) =>
                AssetDatabase.LoadAllAssetsAtPath(ClipsPath).OfType<AnimationClip>().First(c => c.name == name);

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("attackSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);
            // attackSpeed must not start at 0 (it would freeze the Attack state).
            AnimatorControllerParameter[] parameters = controller.parameters;
            parameters.First(p => p.name == "attackSpeed").defaultFloat = 1f;
            controller.parameters = parameters;

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            AnimatorState idle = machine.AddState("Idle", new Vector3(250, 0));
            idle.motion = Clip("Walking");
            idle.speed = 0f; // a frozen stride until we have a real idle clip
            AnimatorState walk = machine.AddState("Walk", new Vector3(250, 80));
            walk.motion = Clip("Walking");
            AnimatorState attack = machine.AddState("Attack", new Vector3(500, 0));
            attack.motion = Clip("Attack");
            attack.speedParameterActive = true;
            attack.speedParameter = "attackSpeed";
            AnimatorState hit = machine.AddState("Hit", new Vector3(500, 80));
            hit.motion = Clip("Hit_Reaction");
            AnimatorState death = machine.AddState("Death", new Vector3(500, 160));
            death.motion = Clip("Dead");
            machine.defaultState = idle;

            Transition(idle, walk, hasExitTime: false).AddCondition(AnimatorConditionMode.Greater, 0.2f, "speed");
            Transition(walk, idle, hasExitTime: false).AddCondition(AnimatorConditionMode.Less, 0.2f, "speed");

            AnimatorStateTransition toAttack = machine.AddAnyStateTransition(attack);
            Configure(toAttack, hasExitTime: false);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "attack");
            AnimatorStateTransition toHit = machine.AddAnyStateTransition(hit);
            Configure(toHit, hasExitTime: false);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "hit");
            AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
            Configure(toDeath, hasExitTime: false);
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "die");

            Transition(attack, idle, hasExitTime: true);
            Transition(hit, idle, hasExitTime: true);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BattleAnimatorBuilder] {ControllerPath}: Idle, Walk, Attack, Hit, Death.");
            return controller;
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, bool hasExitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            Configure(transition, hasExitTime);
            return transition;
        }

        private static void Configure(AnimatorStateTransition transition, bool hasExitTime)
        {
            transition.hasExitTime = hasExitTime;
            transition.exitTime = 0.95f;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = true;
        }
    }
}
