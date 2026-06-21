using UnityEditor;
using UnityEngine;
using UnitySkills;

namespace StudentSimulator.Editor
{
    public static class UnitySkillsServerLauncher
    {
        [MenuItem("Tools/UnitySkills/Start Server From Codex")]
        public static void Start()
        {
            SkillsHttpServer.Start();
            Debug.Log("[StudentSimulator] UnitySkills server start requested.");
        }
    }
}
