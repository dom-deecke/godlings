using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace Godlings
{
    /// <summary>
    /// Connects the agreed demo rules. Floor cells, spawns, and arena limits come
    /// from the sketch asset. Plain colored geometry remains replaceable by the
    /// artist's tiles and boss artwork; it is not a new visual design.
    /// </summary>
    public sealed class DemoGame : MonoBehaviour
    {
        public DemoRules Rules;
        // Serialized references retain both URP shaders in standalone and Web builds.
        public Material FlatMaterial, SpriteMaterial;
        public TextAsset LevelSketch;
        public Sprite FiSprite;
        public Sprite[] EnemySprites;
        public AudioClip FiHurtClip;
        public Sprite MeteorSprite, MeteorShadowSprite;

        private sealed class Enemy
        {
            public GridActor Actor;
            public Health Health;
            public SpriteRenderer View;
        }

        private DemoLevel level;
        private GridBoard board;
        private GridActor player;
        private SpriteRenderer playerSprite;
        private readonly List<Enemy> enemies = new();
        private Health playerHealth, bossHealth;
        private readonly FrameInvulnerability playerInvulnerability = new();
        private readonly List<GameObject> swordViews = new();
        private readonly HashSet<Enemy> swordHitEnemies = new();
        private Vector2Int swordDirection;
        private double swordUntil;
        private bool swordActive, swordHitBoss;
        private GameObject bossView;
        private DemoInput input;
        private Camera viewCamera;
        private Material flatMaterial;
        private RockAttackSchedule rocks;
        private BossSwipeSchedule swipes;
        private readonly List<GameObject> swipeViews = new();
        private readonly List<GameObject> warningViews = new();
        private sealed class CraterMarker
        {
            public GameObject View;
            public double ExpiresAt;
        }
        private readonly List<CraterMarker> craters = new();
        private double shadowStartedAt;
        private DamageAudio damageAudio;
        private Vector2Int aim = Vector2Int.right;
        private Vector2Int heldDirection;
        private double clock, nextStep, nextAttack, nextContact;
        private bool firstHeldStep, arenaActive, ended, won, controlsOpen;
        private bool annotationPaused;
        private string status = "Reach the boss area.";
        private Vector2 controlsScroll, motionScroll;
        public bool Ready { get; private set; }
        public float PlayerCurrentHealth => playerHealth.Current;
        public float BossCurrentHealth => bossHealth.Current;
        public bool HasEnded => ended;
        public bool HasWon => won;
        public Vector2Int PlayerCell => player.Cell;
        public Vector2Int AttackDirection => aim;
        public Vector2Int FirstEnemyCell => enemies[0].Actor.Cell;

        private bool motionOpen;
        private MeteorAudio meteorAudio;
        private DemoRules motionDefaults;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
#endif
            Time.timeScale = 1;
            if (Rules == null || !Rules.HasCompleteCombatValues)
                throw new System.InvalidOperationException("Assign the confirmed DemoRules asset.");
            // Tuning is session-only and never edits the project's rules asset.
            motionDefaults = Rules;
            Rules = Instantiate(Rules);
            level = DemoLevel.Load(LevelSketch);
            board = new GridBoard(level.Floor);
            input = new DemoInput();
            if (FlatMaterial == null || SpriteMaterial == null)
                throw new System.InvalidOperationException("Prepare the demo to assign its URP materials.");
            flatMaterial = new Material(FlatMaterial);
            foreach (Vector2Int cell in level.Floor)
                Tile("Test tile", cell, new Color(187/255f,214/255f,171/255f), -1000, 0.96f);

            var playerObject = new GameObject("Fi");
            player = playerObject.AddComponent<GridActor>();
            player.Initialize(board, "Fi", level.PlayerStart, Rules.StepSeconds, 1);
            var playerArt = new GameObject("Supplied Fi artwork");
            playerArt.transform.SetParent(playerObject.transform,false);
            playerArt.transform.localPosition = Rules.PlayerVisualOffset;
            playerSprite = playerArt.AddComponent<SpriteRenderer>();
            playerSprite.sharedMaterial = SpriteMaterial;
            playerSprite.sprite = FiSprite;
            // Imported sprite has a foot pivot; the actor remains upright.
            playerArt.transform.localScale = Vector3.one * (1.2f / FiSprite.bounds.size.y);
            playerHealth = new Health(Rules.PlayerHealth);
            playerHealth.Died += () => End(false);
            damageAudio = gameObject.AddComponent<DamageAudio>();
            damageAudio.Initialize(FiHurtClip);
            playerHealth.Changed += _ => damageAudio.PlayFiHurt();

            for (int i = 0; i < level.EnemyStarts.Count; i++)
            {
                Vector2Int start = level.EnemyStarts[i];
                var body = new GameObject("Enemy " + (i+1));
                var actor = body.AddComponent<GridActor>();
                actor.Initialize(board, "Enemy"+i, start, Rules.EnemyStepSeconds, 1);
                var visual = new GameObject("Supplied enemy artwork");
                visual.transform.SetParent(body.transform, false);
                visual.transform.localPosition = Rules.EnemyVisualOffset;
                var enemyView = visual.AddComponent<SpriteRenderer>();
                enemyView.sharedMaterial = SpriteMaterial;
                enemyView.sprite = EnemySprites[i % EnemySprites.Length];
                visual.transform.localScale = Vector3.one * (0.95f / enemyView.sprite.bounds.size.y);
                var enemyHealth = new Health(Rules.EnemyHealth);
                enemyHealth.Changed += _ => damageAudio.PlayBurp();
                enemies.Add(new Enemy { Actor = actor, Health = enemyHealth, View = enemyView });
            }

            board.AddActor("Boss", level.BossStart);
            bossView = new GameObject("Boss (large supplied enemy)");
            bossView.transform.position = GridProjection.World(level.BossStart);
            var bossSprite = bossView.AddComponent<SpriteRenderer>();
            bossSprite.sharedMaterial = SpriteMaterial;
            // The green entry is the supplied enemy's original body hue.
            bossSprite.sprite = EnemySprites[4];
            bossSprite.sortingOrder = Depth(bossView.transform.position);
            bossView.transform.localScale = Vector3.one * (1.9f / bossSprite.sprite.bounds.size.y);
            bossHealth = new Health(Rules.BossHealth);
            bossHealth.Died += () => End(true);
            bossHealth.Changed += _ => damageAudio.PlayBossBurp();
            swipes = new BossSwipeSchedule(0,Rules.BossSwipeWarningSeconds,Rules.BossSwipeCooldownSeconds);
            swipes.WarningStarted += ShowSwipeWarning;
            swipes.Impact += SwipeImpact;
            swipes.WarningsCleared += ClearSwipeWarning;

            var cameraObject = new GameObject("Scrolling camera");
            viewCamera = cameraObject.AddComponent<Camera>();
            var cameraData = viewCamera.GetUniversalAdditionalCameraData();
            cameraData.SetRenderer(0);
            cameraData.renderPostProcessing = false;
            viewCamera.allowHDR = false;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            meteorAudio = gameObject.AddComponent<MeteorAudio>();
            meteorAudio.Initialize(Rules.RockWarningSeconds);
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = 4;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color(0.12f,0.12f,0.12f);
            UpdateCamera();
            Ready = true;
        }

        private void Update()
        {
            if (!Ready) return;
            meteorAudio.SetPaused(Time.timeScale == 0);
            damageAudio.SetPaused(Time.timeScale == 0 && !ended);
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame && !input.IsRebinding)
                ToggleControls();
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame && !input.IsRebinding)
                SetAnnotationPause(annotationPaused ? "0" : "1");
            if (ended || controlsOpen || motionOpen || annotationPaused) return;
            playerInvulnerability.AdvanceFrame();
            clock += Time.deltaTime;
            board.AdvanceTime(clock);
            UpdateRockMarkers();
            ProcessMovement(input.Movement());
            if (input.Attack(ref aim) && clock >= nextAttack) Sword();
            UpdateEnemies();

            if (!arenaActive && level.MeteorZone.Count > 0 && player.Cell.x >= level.meteorActivationColumn)
            {
                arenaActive = true;
                status = "Meteors: evade the growing shadow. Craters block movement for two seconds.";
                rocks = new RockAttackSchedule(level.RockCenters(), clock,
                    Rules.RockMinimumIntervalSeconds, Rules.RockIntervalSeconds, Rules.RockWarningSeconds, new System.Random());
                rocks.WarningStarted += ShowWarning;
                rocks.Impact += RockImpact;
                rocks.WarningsCleared += ClearWarnings;
                rocks.TriggerFirstWarningNow();
            }
            if (rocks != null)
            {
                if (rocks.IsWarningDue(clock)) rocks.SetAllowedFootprints(VisibleRockFootprints());
                rocks.Advance(clock);
            }
            if (!ended) swipes.Advance(clock,level.BossStart,player.Cell);
        }

        private void LateUpdate()
        {
            if (!Ready) return;
            if (!ended && !controlsOpen && !motionOpen && !annotationPaused) UpdateSword();
            UpdateCamera();
            playerSprite.sortingOrder = Depth(player.transform.position);
            foreach (Enemy enemy in enemies)
                if (enemy.Actor != null)
                    enemy.View.sortingOrder = Depth(enemy.Actor.transform.position);
        }

        private static int Depth(Vector3 position) => 200 - Mathf.RoundToInt(position.y * 100);

        private void UpdateCamera()
        {
            // Camera tracks position only; it never changes logical grid coordinates.
            viewCamera.transform.position = new Vector3(player.transform.position.x + 3,
                GridProjection.World(new Vector2(0,2.5f)).y + 0.9f, -10);
        }

        private void ProcessMovement(Vector2Int direction)
        {
            if (direction == Vector2Int.zero) { heldDirection = direction; return; }
            if (direction != heldDirection)
            {
                heldDirection = direction;
                nextStep = clock;
                firstHeldStep = true;
            }
            if (player.IsMoving || clock < nextStep) return;
            TryMovePlayer(direction);
            nextStep = clock + (firstHeldStep ? Rules.HoldDelaySeconds : Rules.StepSeconds);
            firstHeldStep = false;
        }

        public bool TryMovePlayer(Vector2Int direction)
        {
            if (ended) return false;
            if (!board.CanCross(player.Cell,direction)) return false;
            Vector2Int destination = player.Cell + direction;
            foreach (Enemy enemy in enemies)
                if (!enemy.Health.IsDead && !enemy.Actor.IsRecoiling && enemy.Actor.Cell == destination) ContactDamage();
            bool moved = player.TryMove(direction);
            if (moved && direction.x != 0) playerSprite.flipX = direction.x < 0;
            return moved;
        }

        private void Sword()
        {
            ClearSword();
            nextAttack = clock+Rules.AttackCooldownSeconds;
            swordDirection = aim;
            swordUntil = clock+0.12;
            swordActive = true;
            swordHitBoss = false;
            swordHitEnemies.Clear();
            for (int distance=1;distance<=3;distance++)
                swordViews.Add(Tile("Moving sword hit cells",player.Cell+swordDirection*distance,
                    new Color(1,1,1,0.35f),-850,0.86f));
        }

        private void UpdateSword()
        {
            if (!swordActive) return;
            if (ended || clock >= swordUntil) { ClearSword(); return; }
            Vector2 origin = GridProjection.Logical(player.transform.position);
            var originCell = new Vector2Int(Mathf.RoundToInt(origin.x),Mathf.RoundToInt(origin.y));
            int reach = 0;
            for (int distance=1;distance<=3;distance++)
            {
                if (!board.IsFloor(originCell+swordDirection*distance)) break;
                reach++;
            }
            for (int i=0;i<swordViews.Count;i++)
            {
                swordViews[i].SetActive(i<reach);
                swordViews[i].transform.position = player.transform.position+GridProjection.World(swordDirection*(i+1));
            }
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health.IsDead || swordHitEnemies.Contains(enemy)) continue;
                if (!AttackPatterns.SwordContains(origin,swordDirection,GridProjection.Logical(enemy.Actor.transform.position),reach)) continue;
                swordHitEnemies.Add(enemy);
                enemy.Health.TakeDamage(Rules.SwordDamage);
                if (enemy.Health.IsDead)
                {
                    enemy.Actor.RemoveFromGrid();
                    Destroy(enemy.Actor.gameObject);
                }
                else enemy.Actor.Recoil(swordDirection,Rules.EnemyRecoilSeconds);
            }
            if (!swordHitBoss && !bossHealth.IsDead && AttackPatterns.SwordContains(origin,swordDirection,level.BossStart,reach))
            {
                swordHitBoss = true;
                bossHealth.TakeDamage(Rules.SwordDamage);
            }
        }

        private void ClearSword()
        {
            swordActive = false;
            foreach (GameObject view in swordViews) if (view != null) Destroy(view);
            swordViews.Clear();
        }

        private void UpdateEnemies()
        {
            foreach (Enemy enemy in enemies)
            {
                if (ended) break;
                if (enemy.Health.IsDead || enemy.Actor.IsMoving) continue;
                if (!EnemyAwareness.CanSense(enemy.Actor.Cell,player.Cell,Rules.EnemySenseRadiusTiles)) continue;
                Vector2Int direction = NextEnemyStep(enemy.Actor.Cell);
                if (direction == Vector2Int.zero) continue;
                if (enemy.Actor.Cell + direction == player.Cell) ContactDamage();
                else enemy.Actor.TryMove(direction);
            }
        }

        private Vector2Int NextEnemyStep(Vector2Int start)
        {
            // Breadth-first search follows the sketch's narrow passage and observes
            // the same corner and occupancy rules as player movement.
            var queue = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int,Vector2Int> { [start] = start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (cell == player.Cell)
                {
                    while (parents[cell] != start) cell = parents[cell];
                    return cell - start;
                }
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var next = cell + new Vector2Int(dx,dy);
                        if (parents.ContainsKey(next) || !board.CanCross(cell,new Vector2Int(dx,dy))) continue;
                        if (next != player.Cell && board.IsOccupied(next)) continue;
                        parents[next] = cell;
                        queue.Enqueue(next);
                    }
            }
            return Vector2Int.zero;
        }

        private void DamageFi(float amount)
        {
            if (playerHealth.IsDead || !playerInvulnerability.TryBeginHit(Rules.PlayerInvincibilityFrames)) return;
            playerHealth.TakeDamage(amount);
        }

        private void ContactDamage()
        {
            if (clock < nextContact || ended) return;
            // Contact is resolved at the approved initial attack cadence, not once
            // per rendering frame. This remains editable through DemoRules.
            nextContact = clock + Rules.AttackCooldownSeconds;
            DamageFi(Rules.EnemyDamage);
        }

        private bool MeteorTouchesEnemy(IReadOnlyList<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                if (cell == level.BossStart) return true;
                foreach (Enemy enemy in enemies)
                    if (!enemy.Health.IsDead && enemy.Actor.Occupies(cell)) return true;
            }
            return false;
        }

        private Vector2Int[][] VisibleRockFootprints()
        {
            int firstSize = UnityEngine.Random.Range(1,4);
            for (int attempt=0;attempt<3;attempt++)
            {
                int size = (firstSize-1+attempt)%3+1;
                var visible = new List<Vector2Int[]>();
                foreach (Vector2Int anchor in level.MeteorZone)
                {
                    Vector2Int[] cells = AttackPatterns.Rock(anchor,size);
                    if (MeteorTouchesEnemy(cells)) continue;
                    bool fits = true;
                    foreach (Vector2Int cell in cells)
                    {
                        Vector3 point = viewCamera.WorldToViewportPoint(GridProjection.World(cell));
                        if (!level.MeteorZone.Contains(cell) || !board.IsWalkable(cell)
                            || point.x < 0.05f || point.x > 0.95f || point.y < 0.05f || point.y > 0.95f)
                        { fits = false; break; }
                    }
                    if (fits) visible.Add(cells);
                }
                if (visible.Count > 0) return visible.ToArray();
            }
            return System.Array.Empty<Vector2Int[]>();
        }

        private void ShowSwipeWarning(IReadOnlyList<Vector2Int> cells)
        {
            ClearSwipeWarning();
            foreach (Vector2Int cell in cells)
                if (board.IsFloor(cell))
                    swipeViews.Add(Tile("Red boss swipe warning",cell,new Color(1,0.15f,0.15f,0.8f),-700,0.92f));
        }

        private void SwipeImpact(IReadOnlyList<Vector2Int> cells)
        {
            ClearSwipeWarning();
            foreach (Vector2Int cell in cells)
                if (cell == player.Cell) DamageFi(Rules.BossSwipeDamage);
        }

        private void ClearSwipeWarning()
        {
            foreach (GameObject marker in swipeViews) if (marker != null) Destroy(marker);
            swipeViews.Clear();
        }

        private void ShowWarning(IReadOnlyList<Vector2Int> cells)
        {
            ClearWarnings();
            meteorAudio.PlayFall(Mathf.RoundToInt(Mathf.Sqrt(cells.Count)));
            shadowStartedAt = clock;
            // Scale the supplied silhouette to the same square footprint as the impact.
            var shadow = MeteorArt("Growing rock-shaped shadow",cells,MeteorShadowSprite,true);
            shadow.transform.localScale = Vector3.one * 0.08f;
            warningViews.Add(shadow);
        }

        private void RockImpact(IReadOnlyList<Vector2Int> cells)
        {
            ClearWarnings();
            // An enemy may enter a warned area during the fall. Cancel rather
            // than land on it, move a warned hitbox, or harm it invisibly.
            if (MeteorTouchesEnemy(cells)) { meteorAudio.Stop(); return; }
            meteorAudio.PlayImpact(Mathf.RoundToInt(Mathf.Sqrt(cells.Count)));
            board.BlockCells(cells,clock + Rules.CraterSeconds);
            var marker = MeteorArt("Supplied meteorite and crater",cells,MeteorSprite,false);
            craters.Add(new CraterMarker { View = marker, ExpiresAt = clock + Rules.CraterSeconds });
            foreach (Vector2Int cell in cells)
                if (cell == player.Cell) DamageFi(Rules.RockDamage);
        }

        private GameObject MeteorArt(string label, IReadOnlyList<Vector2Int> cells, Sprite sprite, bool shadow)
        {
            Vector2 center = Vector2.zero;
            foreach (Vector2Int cell in cells) center += cell;
            center /= cells.Count;
            float size = Mathf.Sqrt(cells.Count);
            if (sprite == null)
                return Tile(label,cells[0],new Color(0.1f,0.1f,0.1f,0.7f),-750,size);
            var root = new GameObject(label);
            root.transform.SetParent(transform,false);
            root.transform.position = GridProjection.World(center);
            var art = new GameObject("Supplied artwork");
            art.transform.SetParent(root.transform,false);
            var view = art.AddComponent<SpriteRenderer>();
            view.sharedMaterial = SpriteMaterial;
            view.sprite = sprite;
            view.sortingOrder = shadow ? -800 : Depth(root.transform.position);
            // Both exports share identical image bounds: the flattened shadow
            // therefore retains the exact horizontal size of the visible rock.
            float width = Mathf.Abs(GridProjection.World(new Vector2(size,0)).x)
                + Mathf.Abs(GridProjection.World(new Vector2(0,size)).x);
            float height = shadow ? Mathf.Abs(GridProjection.World(new Vector2(0,size)).y)
                : width*sprite.bounds.size.y/sprite.bounds.size.x;
            art.transform.localScale = new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
            if (shadow) view.color = new Color(0,0,0,0.65f);
            return root;
        }

        private void UpdateRockMarkers()
        {
            float progress = Mathf.Clamp01((float)(clock-shadowStartedAt)/Rules.RockWarningSeconds);
            foreach (GameObject shadow in warningViews)
                if (shadow != null) shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.08f,1,progress);
            for (int i=craters.Count-1;i>=0;i--)
                if (clock >= craters[i].ExpiresAt)
                {
                    Destroy(craters[i].View);
                    craters.RemoveAt(i);
                }
        }

        private void ClearWarnings()
        {
            foreach (GameObject tile in warningViews) if (tile != null) Destroy(tile);
            warningViews.Clear();
        }

        private void End(bool victory)
        {
            if (ended) return;
            ended = true;
            won = victory;
            rocks?.Stop();
            swipes?.Stop();
            ClearSword();
            meteorAudio.Stop();
            ClearWarnings();
            if (victory) bossView.SetActive(false);
            Time.timeScale = 0;
        }

        private GameObject Tile(string label, Vector2Int cell, Color color, int order, float size)
        {
            var item = new GameObject(label);
            item.transform.SetParent(transform,false);
            item.transform.position = GridProjection.World(cell);
            var mesh = new Mesh();
            float h = size * 0.5f;
            mesh.vertices = new[] { GridProjection.World(new Vector2(-h,-h)),
                GridProjection.World(new Vector2(h,-h)), GridProjection.World(new Vector2(h,h)),
                GridProjection.World(new Vector2(-h,h)) };
            mesh.triangles = new[] { 0,1,2,0,2,3 };
            mesh.colors = new[] { color,color,color,color };
            mesh.RecalculateBounds();
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<GeneratedMeshLifetime>();
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = flatMaterial;
            renderer.sortingOrder = order;
            return item;
        }

        private void Rectangle(Transform parent, Color color, float width, float height)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-width/2,0,0),new Vector3(width/2,0,0),
                new Vector3(width/2,height,0),new Vector3(-width/2,height,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3 };
            mesh.colors = new[] { color,color,color,color };
            mesh.RecalculateBounds();
            parent.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            parent.gameObject.AddComponent<GeneratedMeshLifetime>();
            var renderer = parent.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = flatMaterial;
            renderer.sortingOrder = Depth(parent.position);
        }

        private void ToggleControls()
        {
            if (input.IsRebinding) return;
            controlsOpen = !controlsOpen;
            Time.timeScale = controlsOpen || motionOpen || ended || annotationPaused ? 0 : 1;
        }

        /// <summary>Browser review controls can freeze the game without covering the scene.</summary>
        public void SetAnnotationPause(string paused)
        {
            annotationPaused = paused == "1";
            Time.timeScale = controlsOpen || motionOpen || ended || annotationPaused ? 0 : 1;
        }

        private void OnGUI()
        {
            if (!Ready) return;
            float scale = Mathf.Clamp(Screen.width/1000f,0.65f,2);
            GUI.matrix = Matrix4x4.Scale(Vector3.one*scale);
            float width = Screen.width/scale, height = Screen.height/scale;
            DrawEnemyHealth(scale,height);
            GUILayout.BeginArea(new Rect(15,12,width-30,140));
            GUILayout.Label($"Fi: {playerHealth.Current:0.#}/{playerHealth.Maximum:0.#} HP    Enemies: {enemies.FindAll(e=>!e.Health.IsDead).Count}    Boss: {bossHealth.Current:0.#}/50 HP");
            GUILayout.Label(status);
            GUILayout.Label("WASD / keypad: move   |   Arrows: attack   |   Controller: sticks + right trigger");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(controlsOpen ? "Close controls (F1)" : "Controls / rebind (F1)",GUILayout.Width(210))) ToggleControls();
            if (GUILayout.Button(annotationPaused ? "Resume (F2)" : "Pause to annotate (F2)",GUILayout.Width(190)))
                SetAnnotationPause(annotationPaused ? "0" : "1");
            if (Rules.EnableMotionPanel && GUILayout.Button(motionOpen ? "Close motion panel" : "Motion panel",GUILayout.Width(150)))
                ToggleMotionPanel();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (motionOpen) DrawMotionPanel(width,height);
            if (ended && !controlsOpen)
            {
                GUILayout.BeginArea(new Rect(width/2-170,height/2-70,340,160),GUI.skin.box);
                GUILayout.Label(won ? "Demo complete — boss defeated." : "Fi has fallen.");
                if (Rules.ShowRestartOnDefeat || won)
                    if (GUILayout.Button("Restart")) Restart();
                GUILayout.EndArea();
            }
            if (controlsOpen) DrawControls(width,height);
        }

        private void DrawEnemyHealth(float scale, float screenHeight)
        {
            var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health.IsDead || enemy.Actor == null) continue;
                Vector3 point = viewCamera.WorldToScreenPoint(new Vector3(enemy.View.bounds.center.x, enemy.View.bounds.max.y + 0.1f, 0));
                if (point.z <= 0) continue;
                float x = point.x/scale - 32;
                float y = screenHeight - point.y/scale;
                Color previous = GUI.color;
                GUI.color = new Color(0,0,0,0.85f);
                GUI.DrawTexture(new Rect(x,y,64,7),Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x+1,y+1,62*(enemy.Health.Current/enemy.Health.Maximum),5),Texture2D.whiteTexture);
                GUI.Label(new Rect(x-5,y-23,74,22),$"{enemy.Health.Current:0.#}/{enemy.Health.Maximum:0.#}",labelStyle);
                GUI.color = previous;
            }
        }

        private void ToggleMotionPanel()
        {
            motionOpen = !motionOpen;
            Time.timeScale = controlsOpen || motionOpen || ended || annotationPaused ? 0 : 1;
        }

        private static float TuningSlider(string label, float value, float minimum, float maximum, string unit)
        {
            GUILayout.Label($"{label}: {value:0.00} {unit}");
            return GUILayout.HorizontalSlider(value,minimum,maximum);
        }

        private void DrawMotionPanel(float width, float height)
        {
            GUILayout.BeginArea(new Rect(width-330,115,315,Mathf.Min(560,height-125)),GUI.skin.box);
            GUILayout.Label("Motion controls · temporary");
            GUILayout.Label("Paused. Lower seconds = faster movement.");
            motionScroll = GUILayout.BeginScrollView(motionScroll);
            Rules.StepSeconds = TuningSlider("Fi step",Rules.StepSeconds,0.05f,0.5f,"s/tile");
            Rules.EnemyStepSeconds = TuningSlider("Enemy step",Rules.EnemyStepSeconds,0.1f,1.5f,"s/tile");
            Rules.HoldDelaySeconds = TuningSlider("Initial hold delay",Rules.HoldDelaySeconds,0.01f,0.6f,"s");
            Rules.AttackCooldownSeconds = TuningSlider("Attack cooldown",Rules.AttackCooldownSeconds,0.1f,1f,"s");
            Rules.EnemySenseRadiusTiles = TuningSlider("Enemy sensing",Rules.EnemySenseRadiusTiles,1,10,"tiles");
            Rules.RockMinimumIntervalSeconds = TuningSlider("Meteor minimum interval",Rules.RockMinimumIntervalSeconds,2,10,"s");
            Rules.RockIntervalSeconds = Mathf.Max(Rules.RockIntervalSeconds,Rules.RockMinimumIntervalSeconds);
            Rules.RockIntervalSeconds = TuningSlider("Meteor maximum interval",Rules.RockIntervalSeconds,Rules.RockMinimumIntervalSeconds,10,"s");
            Vector2 fiOffset = Rules.PlayerVisualOffset;
            fiOffset.x = TuningSlider("Fi horizontal offset",fiOffset.x,-0.5f,0.5f,"world units");
            fiOffset.y = TuningSlider("Fi vertical offset",fiOffset.y,-0.5f,0.5f,"world units");
            Rules.PlayerVisualOffset = fiOffset;
            Vector2 offset = Rules.EnemyVisualOffset;
            offset.x = TuningSlider("Enemy horizontal offset",offset.x,-0.5f,0.5f,"world units");
            offset.y = TuningSlider("Enemy vertical offset",offset.y,-0.5f,0.5f,"world units");
            Rules.EnemyVisualOffset = offset;
            GUILayout.EndScrollView();
            if (GUILayout.Button("Reset test values"))
            {
                Rules.PlayerVisualOffset = motionDefaults.PlayerVisualOffset;
                Rules.RockMinimumIntervalSeconds = motionDefaults.RockMinimumIntervalSeconds;
                Rules.RockIntervalSeconds = motionDefaults.RockIntervalSeconds;
                Rules.StepSeconds = motionDefaults.StepSeconds;
                Rules.EnemyStepSeconds = motionDefaults.EnemyStepSeconds;
                Rules.HoldDelaySeconds = motionDefaults.HoldDelaySeconds;
                Rules.AttackCooldownSeconds = motionDefaults.AttackCooldownSeconds;
                Rules.EnemySenseRadiusTiles = motionDefaults.EnemySenseRadiusTiles;
                Rules.EnemyVisualOffset = motionDefaults.EnemyVisualOffset;
            }
            ApplyMotionTuning();
            if (GUILayout.Button("Close and try")) ToggleMotionPanel();
            GUILayout.EndArea();
        }

        /// <summary>Visual offsets affect only the drawing; grid occupancy stays centered.</summary>
        private void ApplyMotionTuning()
        {
            player.SetStepDuration(Rules.StepSeconds);
            playerSprite.transform.localPosition = Rules.PlayerVisualOffset;
            rocks?.SetIntervalRange(Rules.RockMinimumIntervalSeconds,Rules.RockIntervalSeconds);
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health.IsDead || enemy.Actor == null) continue;
                enemy.Actor.SetStepDuration(Rules.EnemyStepSeconds);
                enemy.View.transform.localPosition = Rules.EnemyVisualOffset;
            }
            // Release any outstanding input delay so the new values apply on resuming.
            nextStep = clock;
            nextAttack = clock;
        }

        private void DrawControls(float width, float height)
        {
            GUILayout.BeginArea(new Rect(width/2-270,145,540,Mathf.Max(150,height-160)),GUI.skin.box);
            GUILayout.Label(input.IsRebinding ? "Press a control for " + input.RebindingLabel + " (Escape cancels)" : "Select a binding to change. Simulation is paused.");
            controlsScroll = GUILayout.BeginScrollView(controlsScroll);
            foreach (InputAction action in input.Map.actions)
                for (int i=0;i<action.bindings.Count;i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(action.name,GUILayout.Width(160));
                    GUI.enabled = !input.IsRebinding;
                    if (GUILayout.Button(action.GetBindingDisplayString(i))) input.BeginRebind(action,i);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            GUILayout.EndScrollView();
            GUI.enabled = !input.IsRebinding;
            if (GUILayout.Button("Reset default bindings")) input.ResetBindings();
            if (GUILayout.Button("Close")) ToggleControls();
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDestroy()
        {
            rocks?.Stop();
            swipes?.Stop();
            input?.Dispose();
            Time.timeScale = 1;
            if (Rules != null && Rules != motionDefaults) Destroy(Rules);
            if (flatMaterial != null) Destroy(flatMaterial);
        }
    }
}
