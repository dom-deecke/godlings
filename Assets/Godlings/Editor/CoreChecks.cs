using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Godlings.Editor
{
    /// <summary>Small behavior checks runnable in the Editor or in batch mode.</summary>
    public static class CoreChecks
    {
        [MenuItem("Godlings/Run core checks")]
        public static void Run()
        {
            var isolatedLevel = DemoLevel.Load(new TextAsset("{\"rowsTopToBottom\":[\"#P##B\"],\"bossArena\":{\"minimumCell\":[20,0],\"maximumCellInclusive\":[28,4]}}"));
            Check(isolatedLevel.MeteorZone.Count == 0,"A level without a meteor run must load without inventing a hazard region.");
            var floor = new List<Vector2Int>();
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++) floor.Add(new Vector2Int(x, y));
            var grid = new GridBoard(floor);
            grid.AddActor("Fi", Vector2Int.zero);
            grid.AddActor("enemy", new Vector2Int(2, 0));
            Check(grid.TryBeginStep("Fi", Vector2Int.right), "Fi must move one square.");
            Check(!grid.TryBeginStep("enemy", Vector2Int.left), "A destination must be reserved during a slide.");
            Check(grid.PositionOf("Fi") == Vector2Int.zero, "Occupancy must remain at the origin until arrival.");
            grid.CompleteStep("Fi");
            Check(grid.PositionOf("Fi") == Vector2Int.right, "Fi must arrive at the cell center.");
            Check(!grid.TryBeginStep("Fi", new Vector2Int(2, 0)), "Steps cannot skip cells.");
            Check(!grid.TryBeginStep("Fi", Vector2Int.right), "Occupied cells must block movement.");
            Check(grid.TryBeginStep("Fi", new Vector2Int(1,1)), "Open diagonal steps must work.");
            grid.CompleteStep("Fi");

            var corner = new GridBoard(new[] { Vector2Int.zero, Vector2Int.up, Vector2Int.one });
            corner.AddActor("Fi", Vector2Int.zero);
            Check(!corner.TryBeginStep("Fi", Vector2Int.one), "Walls must prevent diagonal corner cutting.");

            var hazards = new GridBoard(floor);
            hazards.AddActor("Fi",Vector2Int.zero);
            hazards.AddActor("enemy",new Vector2Int(2,0));
            hazards.AdvanceTime(5);
            hazards.BlockCells(new[] { Vector2Int.right },7);
            Check(hazards.IsFloor(Vector2Int.right) && !hazards.IsWalkable(Vector2Int.right),"Craters must block traversal without deleting floor.");
            Check(!hazards.TryBeginStep("Fi",Vector2Int.right) && !hazards.TryBeginStep("enemy",Vector2Int.left),"Craters must block both Fi and enemies.");
            Check(!hazards.CanCross(Vector2Int.zero,Vector2Int.one),"Craters must prevent diagonal corner cutting.");
            hazards.AdvanceTime(6.99);
            Check(!hazards.IsWalkable(Vector2Int.right),"Craters must remain blocked for the full two seconds.");
            hazards.AdvanceTime(7);
            Check(hazards.TryBeginStep("Fi",Vector2Int.right),"Crater tiles must reopen exactly on expiry.");
            hazards.CompleteStep("Fi");
            hazards.BlockCells(new[] { new Vector2Int(1,1) },9);
            hazards.BlockCells(new[] { new Vector2Int(1,1) },8);
            hazards.AdvanceTime(8);
            Check(!hazards.IsWalkable(new Vector2Int(1,1)),"Overlapping craters must retain the longer lifetime.");
            hazards.AdvanceTime(9);
            Check(hazards.IsWalkable(new Vector2Int(1,1)),"Overlapping crater blockage must eventually expire.");

            Vector2Int[] sword = AttackPatterns.Sword(new Vector2Int(4,4), Vector2Int.left);
            Check(sword.Length == 3 && sword[2] == new Vector2Int(1,4) && sword[0] == new Vector2Int(3,4) && sword[1] == new Vector2Int(2,4), "Sword must hit exactly the three cells ahead.");
            Check(AttackPatterns.SnapCardinal(new Vector2(-0.8f,0.3f), Vector2Int.up) == Vector2Int.left, "Controller aim must snap to nearest cardinal.");
            bool diagonalRejected = false;
            try { AttackPatterns.Sword(Vector2Int.zero, Vector2Int.one); }
            catch (ArgumentException) { diagonalRejected = true; }
            Check(diagonalRejected, "Sword attacks must reject diagonals.");
            var rock = new HashSet<Vector2Int>(AttackPatterns.Rock(new Vector2Int(8,8)));
            Check(rock.Count == 9 && rock.Contains(new Vector2Int(7,7)) && rock.Contains(new Vector2Int(9,9)), "Rock footprint must be 3 by 3.");

            for (int size=1;size<=3;size++)
            {
                var footprint = new HashSet<Vector2Int>(AttackPatterns.Rock(Vector2Int.zero,size));
                Check(footprint.Count == size*size,"Meteor sizes must have exactly 1, 4, or 9 hit cells.");
            }
            var variable = new RockAttackSchedule(new[] { Vector2Int.zero },0,2,1.5,new System.Random(1));
            int variableWarnings = 0;
            variable.WarningStarted += cells => { variableWarnings++; Check(cells.Count == 4,"Selected size must survive scheduling."); };
            variable.SetAllowedFootprints(new[] { AttackPatterns.Rock(Vector2Int.zero,2) });
            variable.Advance(2);
            variable.SetAllowedFootprints(Array.Empty<Vector2Int[]>());
            variable.Advance(4);
            Check(variableWarnings == 1,"No safe target must skip the next drop.");

            var schedule = new RockAttackSchedule(new[] { new Vector2Int(8,8) }, 0, 5, 1.5, new System.Random(1));
            int warnings = 0, impacts = 0;
            IReadOnlyList<Vector2Int> warned = null;
            schedule.WarningStarted += cells => { warnings++; warned = cells; };
            schedule.Impact += cells => { impacts++; Check(ReferenceEquals(cells, warned), "Impact must use the warned footprint."); };
            schedule.Advance(4.99);
            Check(warnings == 0, "Boss must wait five seconds.");
            schedule.Advance(5);
            schedule.Advance(6.49);
            Check(warnings == 1 && impacts == 0, "Warnings must precede damage.");
            schedule.Advance(6.5);
            Check(impacts == 1, "Rock must strike after the configured warning duration.");
            schedule.Advance(10);
            Check(warnings == 2, "Boss must repeat on its five-second interval.");
            schedule.Stop();
            schedule.Advance(11.5);
            Check(impacts == 1, "Defeating the boss must cancel pending impacts.");

            var randomSchedule = new RockAttackSchedule(new[] { new Vector2Int(8,8) },0,2,10,1.5,new System.Random(9));
            double previousWarning = 0, testTime = 0, shortest = 10, longest = 0;
            int randomWarnings = 0;
            randomSchedule.WarningStarted += _ => {
                double gap = testTime-previousWarning;
                Check(gap >= 2 && gap <= 10.02,"Random meteor intervals must remain between 2 and 10 seconds.");
                shortest = Math.Min(shortest,gap); longest = Math.Max(longest,gap);
                previousWarning = testTime; randomWarnings++;
            };
            for (int tick=1;tick<=30000;tick++) { testTime=tick*0.01; randomSchedule.Advance(testTime); }
            Check(randomWarnings > 20 && longest-shortest > 1,"Meteor intervals must vary rather than follow a fixed timer.");

            var swipe = new BossSwipeSchedule(0,2,5);
            int swipeWarnings = 0, swipeHits = 0;
            IReadOnlyList<Vector2Int> swipeCells = null;
            swipe.WarningStarted += cells => { swipeWarnings++; swipeCells=cells; };
            swipe.Impact += cells => { swipeHits++; Check(ReferenceEquals(cells,swipeCells),"Swipe must retain its warned footprint after Fi evades."); };
            swipe.Advance(0,Vector2Int.zero,Vector2Int.one);
            Check(swipeWarnings == 0,"Diagonal proximity alone must not trigger the swipe.");
            swipe.Advance(0,Vector2Int.zero,new Vector2Int(-3,0));
            Check(swipeWarnings == 0,"Swipe trigger must not extend beyond two tiles.");
            swipe.Advance(0,Vector2Int.zero,new Vector2Int(-2,0));
            Check(swipeWarnings == 1 && swipeCells.Count == 12,"Swipe must warn across a six-by-two rectangle.");
            foreach (Vector2Int cell in swipeCells) Check(cell.x >= -2 && cell.x <= -1,"Swipe must face Fi in the two rows ahead.");
            swipe.Advance(1.99,Vector2Int.zero,new Vector2Int(3,0));
            Check(swipeHits == 0,"Swipe must allow the full two-second warning.");
            swipe.Advance(2,Vector2Int.zero,new Vector2Int(3,0));
            Check(swipeHits == 1,"Swipe must strike after two seconds.");
            swipe.Advance(4.99,Vector2Int.zero,Vector2Int.left);
            Check(swipeWarnings == 1,"Swipe must respect its five-second cooldown.");
            swipe.Advance(5,Vector2Int.zero,Vector2Int.left);
            Check(swipeWarnings == 2,"Swipe may warn again after five seconds.");
            swipe.Stop(); swipe.Advance(7,Vector2Int.zero,Vector2Int.left);
            Check(swipeHits == 1,"Ending the encounter must cancel pending swipes.");

            var protection = new FrameInvulnerability();
            Check(protection.TryBeginHit(10),"An unprotected hit must begin invincibility.");
            for (int frame=0;frame<10;frame++)
            {
                Check(!protection.TryBeginHit(10),"Fi must reject damage throughout all ten protected frames.");
                protection.AdvanceFrame();
            }
            Check(protection.TryBeginHit(10),"Fi must become vulnerable after ten gameplay frames.");
            Check(!AttackPatterns.SwordContains(Vector2.zero,Vector2Int.right,new Vector2(4,0)),"Sword must not reach an old fixed area ahead.");
            Check(AttackPatterns.SwordContains(new Vector2(0.75f,0),Vector2Int.right,new Vector2(4,0)),"Sword hurtbox must follow Fi during a slide.");
            Check(!AttackPatterns.SwordContains(new Vector2(0,1),Vector2Int.right,new Vector2(2,0)),"Sword hurtbox must follow Fi vertically.");
            var interrupted = new GridBoard(floor);
            interrupted.AddActor("enemy",Vector2Int.one);
            Check(interrupted.TryBeginStep("enemy",Vector2Int.right),"Enemy must reserve an approach step.");
            interrupted.CancelStep("enemy");
            Check(!interrupted.IsOccupied(new Vector2Int(2,1)),"Recoil must release the interrupted step's reservation.");
            Check(interrupted.TryBeginStep("enemy",Vector2Int.left),"Recoil must reserve one tile backwards.");
            interrupted.CompleteStep("enemy");
            Check(interrupted.PositionOf("enemy") == Vector2Int.up,"Recoil must end exactly one tile backwards.");

            var health = new Health(10);
            health.TakeDamage(2);
            health.TakeDamage(3);
            Check(health.Current == 5, "Enemy and rock damage must subtract the confirmed amounts.");
            int deaths = 0;
            health.Died += () => deaths++;
            health.TakeDamage(100);
            health.TakeDamage(2);
            Check(health.Current == 0 && deaths == 1, "Death must occur once and health must not become negative.");

            var enemyHealth = new Health(3);
            enemyHealth.TakeDamage(1.5f);
            Check(enemyHealth.Current == 1.5f && !enemyHealth.IsDead, "One sword hit must leave 1.5 enemy health.");
            enemyHealth.TakeDamage(1.5f);
            Check(enemyHealth.IsDead, "Two sword hits must defeat a regular enemy.");
            var bossHealth = new Health(50);
            for (int i = 0; i < 33; i++) bossHealth.TakeDamage(1.5f);
            Check(bossHealth.Current == 0.5f && !bossHealth.IsDead, "Boss must survive 33 initial sword hits.");
            bossHealth.TakeDamage(1.5f);
            Check(bossHealth.IsDead, "The 34th initial sword hit must defeat the boss.");
            Check(EnemyAwareness.CanSense(Vector2Int.zero,new Vector2Int(4,0),4), "Enemy must sense Fi at the range boundary.");
            Check(!EnemyAwareness.CanSense(Vector2Int.zero,new Vector2Int(4,1),4), "Enemy must not sense Fi beyond the range boundary.");
            Check(!EnemyAwareness.CanSense(Vector2Int.zero,new Vector2Int(5,0),4), "Out-of-range pursuit must stop.");
            Check(EnemyAwareness.CanSense(new Vector2Int(5,0),new Vector2Int(2,0),4), "Fi may be sensed again on re-entering range.");
            Debug.Log("GODLINGS_CORE_CHECKS_PASSED");
        }

        public static void PrepareAndCheck()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            const string path = "Assets/Godlings/DemoRules.asset";
            if (AssetDatabase.LoadAssetAtPath<DemoRules>(path) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DemoRules>(), path);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Godlings/Art" }))
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Run();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
