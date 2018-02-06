using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Game
    {
        public enum Mode
        {
            Invalid,
            Game,
            Inventory,
            Exit,
            Look,
            Throw,
            OpenDoor,
            CloseDoor
        }

        public static Game Instance;

        public Mode mode;
        public Unit player;
        public Map map;
        public bool player_acted;
        
        List<Unit> units = new List<Unit>();
        Pos screen_size;
        Queue<string> texts = new Queue<string>();
        string player_name;
        
        const int RADIUS = 8;
        TextWriter log;
        Inventory inv = new Inventory();

        Pos look_pos;
        float look_timer;
        bool look_blink;

        Item throw_prev;
        int throw_index;
        Unit throw_target;

        public bool ChangeMode(Mode new_mode)
        {
            if (mode == new_mode)
                return false;
            mode = new_mode;
            return true;
        }

        public void StartThrow(int index, Item item)
        {
            mode = Mode.Throw;
            throw_index = index;
            throw_prev = item;
            PickThrowTarget();
        }

        public void Init()
        {
            Instance = this;

            // init log
            log = new StreamWriter("log.txt");
            log.WriteLine("Hunters - version 0");
            log.WriteLine(DateTime.Now.ToString());
            log.Flush();

            Item.LoadItems();

            // init console
            Console.Init("Hunters", 70, 26, 20);
            screen_size = new Pos(70, 20);
            map = new Map(50, 50);

            // start menu
            Console.Clear();
            string logo = @" .-. .-..-. .-..-. .-. _______ ,---.  ,---.    .---. 
 | | | || | | ||  \| ||__   __|| .-'  | .-.\  ( .-._)
 | `-' || | | ||   | |  )| |   | `-.  | `-'/ (_) \   
 | .-. || | | || |\  | (_) |   | .-'  |   (  _  \ \  
 | | |)|| `-')|| | |)|   | |   |  `--.| |\ \( `-'  ) 
 /(  (_)`---(_)/(  (_)   `-'   /( __.'|_| \)\`----'  
(__)          (__)            (__)        (__)       ";

            Console.WriteText2(logo);
            Console.WriteText2("\nCreated by Tomashu - Version 0\n\nYour name: ");
            player_name = Console.GetText();
            if (player_name == null)
                Environment.Exit(0);

            // try load save
            if(!TryLoad())
                NewGame();
        }

        void NewGame()
        {
            mode = Mode.Game;

            player = new Unit { pos = new Pos(5, 5), ai = false };
            player.weapon = new GameItem(Item.Find("knife"), 1);
            player.armor = new GameItem(Item.Find("ljacket"), 1);
            player.items.Add(new GameItem(Item.Find("pistol"), 1, 10));
            player.items.Add(new GameItem(Item.Find("ammo9mm"), 30));
            player.items.Add(new GameItem(Item.Find("p_hp"), 2));
            player.items.Add(new GameItem(Item.Find("stuff"), 15));
            Tile t = map[player.pos];
            t.unit = player;
            t.type = Tile.Type.Empty;
            units = new List<Unit>();
            units.Add(player);

            Unit ai = new Unit { pos = new Pos(20, 10), ai = true };
            t = map[ai.pos];
            t.unit = ai;
            t.type = Tile.Type.Empty;
            units.Add(ai);

            ai = new Unit { pos = new Pos(10, 20), ai = true };
            t = map[ai.pos];
            t.unit = ai;
            t.type = Tile.Type.Empty;
            units.Add(ai);

            AddText("Welcome {0}! Press ? for controls.", player_name);

            // reset on new game/load
            throw_prev = null;
            throw_target = null;
            map.CalculateFov(player.pos, RADIUS);

            Log("Started new game.");
        }

        public bool HandleQuitDialog()
        {
            ConsoleKeyInfo k = Console.ReadKey();
            if (k.Key == ConsoleKey.Y)
            {
                Log("Quit without saving.");
                mode = Mode.Exit;
                return false;
            }
            else if (k.Key == ConsoleKey.N || k.Key == ConsoleKey.Escape)
                return false;
            return true;
        }

        void Attack(Unit a, Unit b)
        {
            string action;
            if (a.weapon != null)
                action = "stab";
            else
                action = (Utils.K2() ? "punch" : "kick");

            string s;
            if(Utils.Chance(70 + (a.ai ? 0 : 5)))
            {
                // hit
                int dmg = Utils.Random(1, 5) + (a.ai ? 0 : 1);
                b.hp -= dmg;
                if(b.hp <= 0)
                {
                    if (b.ai)
                        s = string.Format("You {0} enemy for {1} damage and kills him.", action, dmg);
                    else
                        s = string.Format("Enemy {0} you for {1} damage and kills you.", action, dmg);

                    map[b.pos].unit = null;
                    units.Remove(b);
                }
                else
                {
                    if (b.ai)
                        s = string.Format("You {0} enemy for {1} damage.", action, dmg);
                    else
                        s = string.Format("Enemy {0} you for {1} damage.", action, dmg);
                }
            }
            else
            {
                // miss
                if (b.ai)
                    s = string.Format("You try to {0} enemy but misses.", action);
                else
                    s = string.Format("Enemy try to {0} you but misses.", action);
            }
            AddText(s);
        }

        void PickThrowTarget()
        {
            var targets = units.Where(x => x != player)
                .Select(unit => new { unit, dist = unit.pos.Distance(player.pos) })
                .Where(x => x.dist < 12);
            if(!targets.Any(x => x.unit == throw_target))
            {
                // unit don't have old target, chose nearest
                if(targets.Any())
                {
                    throw_target = targets.MaxBy(x => x.dist).unit;
                    look_pos = throw_target.pos;
                }
                else
                {
                    throw_target = null;
                    look_pos = player.pos;
                }
            }
            else
            {
                // use old target
                look_pos = throw_target.pos;
            }

            look_timer = 0;
            look_blink = true;
        }

        bool GetDir(ConsoleKey k, out Dir dir)
        {
            if (k == ConsoleKey.NumPad1)
                dir = Dir.Dir1;
            else if (k == ConsoleKey.NumPad2 || k == ConsoleKey.DownArrow)
                dir = Dir.Dir2;
            else if (k == ConsoleKey.NumPad3)
                dir = Dir.Dir3;
            else if (k == ConsoleKey.NumPad4 || k == ConsoleKey.LeftArrow)
                dir = Dir.Dir4;
            else if (k == ConsoleKey.NumPad6 || k == ConsoleKey.RightArrow)
                dir = Dir.Dir6;
            else if (k == ConsoleKey.NumPad7)
                dir = Dir.Dir7;
            else if (k == ConsoleKey.NumPad8 || k == ConsoleKey.UpArrow)
                dir = Dir.Dir8;
            else if (k == ConsoleKey.NumPad9)
                dir = Dir.Dir9;
            else
                dir = Dir.None;

            return (dir != Dir.None);
        }

        bool UpdatePlayer()
        {
            Dir dir = Dir.None;

            ConsoleKeyInfo k = Console.ReadKey();
            if(GetDir(k.Key, out dir))
            {
                DirInfo di = dirs[(int)dir];
                Pos new_pos = player.pos + di.change;

                if (new_pos.x < 0 || new_pos.y < 0 || new_pos.x >= map.w || new_pos.y >= map.h)
                {
                    // can't move, out of world
                }
                else
                {
                    Tile t = map[new_pos];
                    if (t.unit != null)
                    {
                        Attack(player, t.unit);
                        return true;
                    }
                    else if (map.CanMove(player.pos, new_pos, di.diagonal))
                    {
                        t.unit = player;
                        map[player.pos].unit = null;
                        player.pos = new_pos;
                        map.CalculateFov(player.pos, RADIUS);
                        if (t.items != null)
                        {
                            if (t.items.Count == 1)
                            {
                                if (t.items[0].count == 1)
                                    AddText("There is {0} on ground.", t.items[0].item.name);
                                else
                                    AddText("There are {0} {1}s ground.", t.items[0].count, t.items[0].item.name);
                            }
                            else
                                AddText("There are many items on ground.");
                        }
                        return true;
                    }
                    else if (t.type == Tile.Type.Door && (t.flags & Tile.Flags.Open) == 0)
                    {
                        // open doors
                        t.flags |= Tile.Flags.Open;
                        map.CalculateFov(player.pos, RADIUS);
                        AddText("You opened door.");
                    }
                }
            }
            else if (k.KeyChar == 'Q')
            {
                ShowDialog("$amQuit WITHOUT saving?\n(y/n)", HandleQuitDialog);
            }
            else if(k.KeyChar == 'S')
            {
                SaveGame();
            }
            else if (k.KeyChar == '?')
            {
                ShowDialog("$amControls\n==========\n$a"
                    + "lNumpad 1-9 - walk around, Numpad 5 - wait, l - look\n"
                    + "t - throw\n"
                    + "o - open door, c - close door\n"
                    + "i - inventory, e - equip, r - remove, u - use, d - drop, g/G - get\n"
                    + "? - controls, S - save, Q - quit");
            }
            else if (k.Key == ConsoleKey.NumPad5)
            {
                // wait turn
                return true;
            }
            else if (k.KeyChar == 'i')
                inv.Show(Inventory.Action.None);
            else if (k.KeyChar == 'd')
                inv.Show(Inventory.Action.Drop);
            else if (k.KeyChar == 'g' || k.KeyChar == 'G')
            {
                Tile t = map[player.pos];
                if (t.items == null)
                    ShowDialog("There is nothing on ground.");
                else if (t.items.Count == 1 && k.KeyChar == 'g')
                {
                    player.AddItem(t.items[0]);
                    if (t.items[0].count == 1)
                        AddText("You picked {0} from ground.", t.items[0].item.name);
                    else
                        AddText("You picked {0} {1}s from ground.", t.items[0].count, t.items[0].item.name);
                    t.items = null;
                    return true;
                }
                else
                    inv.ShowGet(t.items);
            }
            else if (k.KeyChar == 'e')
            {
                // equip item
                inv.Show(Inventory.Action.Equip);
            }
            else if (k.KeyChar == 'r')
            {
                // remove item
                inv.Show(Inventory.Action.Remove);
            }
            else if (k.KeyChar == 'u')
            {
                // use item
                inv.Show(Inventory.Action.Use);
            }
            else if (k.KeyChar == 'l')
            {
                // look around
                mode = Mode.Look;
                look_pos = player.pos;
                look_timer = 0;
                look_blink = true;
            }
            else if (k.KeyChar == 't')
            {
                // throw, use last throwable
                if (throw_prev != null)
                {
                    var to_throw = player.items.GetIndexes().Where(x => x.item.item == throw_prev).SingleOrDefault();
                    if (to_throw.item == null)
                        throw_prev = null;
                    else
                    {
                        throw_index = to_throw.index;
                        mode = Mode.Throw;
                        PickThrowTarget();
                    }
                }
                if (throw_prev == null)
                    inv.Show(Inventory.Action.Throw);
            }
            else if (k.KeyChar == 'T')
            {
                // throw, pick what to throw
                inv.Show(Inventory.Action.Throw);
            }
            else if (k.KeyChar == 'o')
            {
                var tiles = map.GetNearTiles(player.pos).Where(x => x.type == Tile.Type.Door && (x.flags & Tile.Flags.Open) == 0);
                if (tiles.Any())
                {
                    if (tiles.Count() == 1)
                    {
                        tiles.First().flags |= Tile.Flags.Open;
                        map.CalculateFov(player.pos, RADIUS);
                        AddText("You opened door.");
                        return true;
                    }
                    else
                        mode = Mode.OpenDoor;
                }
                else
                    ShowDialog("No nearby doors to open.");
            }
            else if (k.KeyChar == 'c')
            {
                var tiles = map.GetNearTiles(player.pos).Where(x => x.type == Tile.Type.Door && (x.flags & Tile.Flags.Open) != 0);
                if(tiles.Any())
                {
                    if(tiles.Count() == 1)
                    {
                        tiles.First().flags &= ~Tile.Flags.Open;
                        map.CalculateFov(player.pos, RADIUS);
                        AddText("You closed door.");
                        return true;
                    }
                    else
                        mode = Mode.CloseDoor;
                }
                else
                    ShowDialog("No nearby doors to close.");
            }

            return false;
        }

        enum Dir
        {
            Dir1,
            Dir2,
            Dir3,
            Dir4,
            Dir6,
            Dir7,
            Dir8,
            Dir9,
            None
        }

        class DirInfo
        {
            public Dir dir;
            public Pos change;
            public Dir other, other2;
            public bool diagonal;

            public DirInfo(Dir _dir, Pos _change, Dir _other, Dir _other2, bool _diagonal)
            {
                dir = _dir;
                change = _change;
                other = _other;
                other2 = _other2;
                diagonal = _diagonal;
            }
        };

        static DirInfo[] dirs = new DirInfo[] {
            new DirInfo (Dir.Dir1, new Pos(-1,1), Dir.Dir2, Dir.Dir4, true),
            new DirInfo (Dir.Dir2, new Pos(0,1), Dir.Dir1, Dir.Dir3, false),
            new DirInfo (Dir.Dir3, new Pos(1,1), Dir.Dir2, Dir.Dir6, true),
            new DirInfo (Dir.Dir4, new Pos(-1,0), Dir.Dir1, Dir.Dir7, false),
            new DirInfo (Dir.Dir6, new Pos(1,0), Dir.Dir3, Dir.Dir9, false),
            new DirInfo (Dir.Dir7, new Pos(-1,-1), Dir.Dir4, Dir.Dir8, true),
            new DirInfo (Dir.Dir8, new Pos(0,-1), Dir.Dir7, Dir.Dir9, false),
            new DirInfo (Dir.Dir9, new Pos(1,-1), Dir.Dir6, Dir.Dir8, true),
        };

        void UpdateUnits()
        {
            foreach(Unit u in units)
            {
                if (!u.ai)
                    continue;

                if(player.hp > 0)
                {
                    int dist = u.pos.Distance(player.pos);
                    if(dist == 1)
                    {
                        // attack
                        Attack(u, player);
                        if (player.hp <= 0)
                            break;
                    }
                    else if(dist <= 5)
                    {
                        // move
                        Dir dir;
                        if(u.pos.x > player.pos.x)
                        {
                            if (u.pos.y > player.pos.y)
                                dir = Dir.Dir7;
                            else if (u.pos.y < player.pos.y)
                                dir = Dir.Dir1;
                            else
                                dir = Dir.Dir4;
                        }
                        else if(u.pos.x < player.pos.x)
                        {
                            if (u.pos.y > player.pos.y)
                                dir = Dir.Dir9;
                            else if (u.pos.y < player.pos.y)
                                dir = Dir.Dir3;
                            else
                                dir = Dir.Dir6;
                        }
                        else if (u.pos.y > player.pos.y)
                            dir = Dir.Dir8;
                        else
                            dir = Dir.Dir2;

                        DirInfo info = dirs[(int)dir];
                        Pos new_pos = u.pos + info.change;
                        bool ok = false;
                        
                        if(map.CanMove(u.pos, new_pos, info.diagonal))
                            ok = true;
                        else
                        {
                            bool first = Utils.K2();
                            DirInfo info2 = dirs[(int)(first ? info.other : info.other2)];
                            new_pos = u.pos + info2.change;
                            if(map.CanMove(u.pos, new_pos, info2.diagonal))
                                ok = true;
                            else
                            {
                                info2 = dirs[(int)(first ? info.other2 : info.other)];
                                new_pos = u.pos + info2.change;
                                if (map.CanMove(u.pos, new_pos, info2.diagonal))
                                    ok = true;
                            }
                        }

                        if(ok)
                        {
                            map[u.pos].unit = null;
                            u.pos = new_pos;
                            map[u.pos].unit = u;
                        }
                    }
                }                
            }
        }

        public bool Update()
        {
            float dt = Console.Update();
            Debug.Print("Dt: {0}\n", dt);

            if (have_dialog)
                UpdateDialog();
            else
            {
                if (mode == Mode.Game)
                {
                    if (player_acted || UpdatePlayer())
                    {
                        player_acted = false;
                        UpdateUnits();

                        if (player.hp <= 0)
                            ShowDialogAction(string.Format("$amR. I. P.\n{0} 1990 - 2015", player_name), () => { mode = Mode.Exit; });
                    }
                }
                else if (mode == Mode.Inventory)
                    inv.Update();
                else if (mode == Mode.Look || mode == Mode.Throw)
                    return UpdateLook(dt);
                else if (mode == Mode.OpenDoor || mode == Mode.CloseDoor)
                    return UpdatePickDir(dt);
            }

            return true;
        }

        bool UpdateLook(float dt)
        {
            bool blink = false;

            look_timer += dt;
            if (look_timer >= 0.33f)
            {
                look_timer = 0;
                look_blink = !look_blink;
                blink = true;
            }

            ConsoleKeyInfo? ka = Console.ReadKey2();
            if (ka == null)
                return blink;

            Dir dir = Dir.None;
            ConsoleKeyInfo k = ka.Value;

            if(GetDir(k.Key, out dir))
            {
                Pos change = dirs[(int)dir].change;
                if (k.Modifiers == ConsoleModifiers.Shift)
                    change *= 5;
                look_pos += change;
                if (look_pos.x < 0)
                    look_pos.x = 0;
                else if (look_pos.x >= map.w)
                    look_pos.x = map.w - 1;
                if (look_pos.y < 0)
                    look_pos.y = 0;
                else if (look_pos.y >= map.h)
                    look_pos.y = map.h - 1;
            }
            else if (k.KeyChar == '?')
            {
                ShowDialog("$amLook controls\nArrows/numpad - navigate\nl - examine tile\nc - center on yourself\n"
                    + "Escape - exit");
            }
            else if (k.KeyChar == 'l')
            {
                Tile t = map[look_pos];
                if ((t.flags & Tile.Flags.Known) != 0)
                {
                    List<string> items = new List<string>();

                    if (t.type == Tile.Type.Wall)
                        items.Add("wall");

                    if ((t.flags & Tile.Flags.Lit) != 0)
                    {
                        if (t.type == Tile.Type.Door)
                        {
                            if ((t.flags & Tile.Flags.Open) != 0)
                                items.Add("open door");
                            else
                                items.Add("closed door");
                        }

                        if (t.unit != null)
                        {
                            if (t.unit.ai)
                                items.Add("enemy");
                            else
                                items.Add("you");
                        }

                        if (t.items != null)
                        {
                            if (t.items.Count == 1)
                            {
                                if (t.items[0].count == 1)
                                    items.Add(t.items[0].item.name);
                                else
                                    items.Add(string.Format("{0} {1}s", t.items[0].count, t.items[0].item.name));
                            }
                            else
                                items.Add("many items");
                        }
                    }
                    else
                    {
                        if (t.type == Tile.Type.Door)
                        {
                            if ((t.flags & Tile.Flags.LastOpen) != 0)
                                items.Add("open door");
                            else
                                items.Add("closed door");
                        }
                    }

                    if (items.Count == 0)
                        ShowDialog("There is nothing there.");
                    else
                    {
                        StringBuilder s = new StringBuilder("There is ");
                        s.Join(items);
                        if (look_pos == player.pos)
                            s.Append(" here.");
                        else
                            s.Append(" there.");
                        ShowDialog(s.ToString());
                    }
                }
                else
                    ShowDialog("You don't know what is there.");
            }
            else if (k.KeyChar == 'c')
            {
                look_pos = player.pos;
            }
            else if (k.Key == ConsoleKey.Escape)
            {
                mode = Mode.Game;
            }
            else
                return blink;

            return true;
        }

        bool UpdatePickDir(float dt)
        {
            ConsoleKeyInfo k = Console.ReadKey();
            Dir dir = Dir.None;

            if(GetDir(k.Key, out dir))
            {
                Tile t = map.GetTileSafe(player.pos + dirs[(int)dir].change);
                bool ok = true;
                if (t.type != Tile.Type.Door)
                    ok = false;
                else
                {
                    if(mode == Mode.OpenDoor)
                    {
                        if ((t.flags & Tile.Flags.Open) == 0)
                        {
                            // door is closed, open it
                            t.flags |= Tile.Flags.Open;
                            map.CalculateFov(player.pos, RADIUS);
                            player_acted = true;
                            mode = Mode.Game;
                            AddText("You opened door.");
                            return true;
                        }
                        else
                            ok = false;
                    }
                    else
                    {
                        if ((t.flags & Tile.Flags.Open) != 0)
                        {
                            // door is open, close it
                            t.flags &= ~Tile.Flags.Open;
                            map.CalculateFov(player.pos, RADIUS);
                            player_acted = true;
                            mode = Mode.Game;
                            AddText("You closed door.");
                            return true;
                        }
                        else
                            ok = false;
                    }
                }

                if (!ok)
                    ShowDialog(string.Format("There is no door there to {0}.", mode == Mode.OpenDoor ? "open" : "close"));
            }
            else if(k.KeyChar == '?')
            {
                ShowDialog(string.Format("Press direction key to {0} door.\nEscape to cancel action.",
                    mode == Mode.OpenDoor ? "open" : "close"));
                return true;
            }
            else if(k.Key == ConsoleKey.Escape)
            {
                mode = Mode.Game;
                return true;
            }

            return false;
        }

        public void Draw()
        {
            Console.Clear();

            if(mode == Mode.Invalid)
            {
                if(have_dialog)
                    DrawDialog();

                Console.Draw();
                return;
            }

            Pos my_pos = ((mode == Mode.Look || mode == Mode.Throw) ? look_pos : player.pos);
            Pos offset = new Pos(my_pos.x - screen_size.x / 2, my_pos.y - screen_size.y / 2);
            offset.y -= 1;

            short bkg = (((short)ConsoleColor.DarkGray) << 4) | ((short)ConsoleColor.Black);
            string text = string.Format("{4} L:1 XP:0% HP:{0}/{1} W:{2} A:{3}",
                player.hp,
                player.hpmax,
                player.weapon != null ? player.weapon.item.name : "fists",
                player.armor != null ? player.armor.item.name : "clothes",
                player_name); 

            // gui
            for (int i = 0; i < Console.Width; ++i)
                Console.buf[i].Attributes = bkg;
            for (int i = 0; i < text.Length; ++i)
                Console.buf[i].Char.UnicodeChar = text[i];
            for (int y = Console.Height - 5; y < Console.Height; ++y)
            {
                for (int x = 0; x < Console.Width; ++x)
                    Console.buf[x + y * Console.Width].Attributes = bkg;
            }
            int count = Math.Min(texts.Count, 5);
            for (int i = 0; i < count; ++i)
            {
                int y = 21 + i;
                string s = texts.ElementAt(texts.Count-count+i);
                for (int x = 0; x < s.Length; ++x)
                    Console.buf[x + y * Console.Width].Char.UnicodeChar = s[x];
            }

            // map
            map.Draw(screen_size, offset, new Pos(0, -1));

            offset.y -= 1;

            if (mode == Mode.Look || mode == Mode.Throw)
            {
                if (mode == Mode.Throw)
                {
                    int type = 0;
                    foreach (Pos pt in Utils.Line(player.pos, look_pos))
                    {
                        ConsoleColor color;
                        if (type == 0)
                        {
                            type = 1;
                            continue;
                        }
                        else if (type >= 2)
                        {
                            type = 3;
                            color = ConsoleColor.Red;
                        }
                        else if (map[pt].type == Tile.Type.Wall || map[pt].unit != null)
                        {
                            type = 2;
                            color = ConsoleColor.Yellow;
                        }
                        else
                            color = ConsoleColor.Gray;
                        Console.buf[pt.x - offset.x + (pt.y - offset.y) * Console.Width].Set('*', (short)color);
                    }
                }

                string text2 = (mode == Mode.Look ? "Look mode" : "Throw mode");
                Console.WriteText(text2, new Pos((Console.Width - text2.Length) / 2, Console.Height - 1),
                        Console.MakeColor(ConsoleColor.Black, ConsoleColor.White));
                if (look_blink)
                {
                    int off = my_pos.x - offset.x + (my_pos.y - offset.y) * Console.Width;
                    Console.buf[off].Attributes = (short)~Console.buf[off].Attributes;
                }
            }
            else if(mode == Mode.OpenDoor || mode == Mode.CloseDoor)
            {
                string text2 = (mode == Mode.OpenDoor ? "Select door to open" : "Select door to close");
                Console.WriteText(text2, new Pos((Console.Width - text2.Length) / 2, Console.Height - 1),
                        Console.MakeColor(ConsoleColor.Black, ConsoleColor.White));
            }

            if (mode == Mode.Inventory)
                inv.Draw();

            if (have_dialog)
                DrawDialog();

            Console.Draw();
        }

        class Line
        {
            public enum Align
            {
                Left = 0,
                Center = 1,
                Right = 2
            };

            public string text;
            public Align align;
        };

        public bool have_dialog;
        List<Line> lines = new List<Line>();
        Func<bool> dialog_f;
        Pos dialog_size;

        void AddLine(StringBuilder line, ref int w, Line.Align line_align)
        {
            foreach(string s in line.ToString().SplitText(screen_size.x - 2, true))
            {
                lines.Add(new Line() { text = s, align = line_align });
                if (s.Length > w)
                    w = s.Length;
            }
        }

        public void ShowDialogAction(string text, Action a)
        {
            Func<bool> f = () =>
            {
                ConsoleKeyInfo k = Console.ReadKey();
                if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)
                {
                    have_dialog = false;
                    a();
                    return false;
                }
                else
                    return true;
            };

            ShowDialog(text, f);
        }

        public void ShowDialog(string text, Func<bool> f = null)
        {
            lines.Clear();
            StringBuilder line_text = new StringBuilder();
            int w = 0;
            Line.Align align = Line.Align.Left;

            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                if (c == '$')
                {
                    ++i;
                    c = text[i];
                    if(c == 'a')
                    {
                        // align
                        ++i;
                        c = text[i];
                        if (c == 'l')
                            align = Line.Align.Left;
                        else if (c == 'm')
                            align = Line.Align.Center;
                        else if (c == 'r')
                            align = Line.Align.Right;
                        else
                            throw new Exception(string.Format("Unkown format string $a{0}.", c));
                    }
                    else if(c == '$')
                        line_text.Append(c);
                    else
                        throw new Exception(string.Format("Unknown format string ${0}.", c));                        
                }
                else if (c == '\n')
                {
                    AddLine(line_text, ref w, align);
                    line_text.Clear();
                }
                else
                    line_text.Append(c);
            }

            AddLine(line_text, ref w, align);

            dialog_size = new Pos(w + 1, lines.Count + 1);
            have_dialog = true;
            dialog_f = f;
        }

        public void DrawWindow(Pos pos, Pos size, short bkg)
        {
            int left = pos.x,
                right = pos.x + size.x,
                top = pos.y,
                bottom = pos.y + size.y;

            // background
            for (int y = top; y <= bottom; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    Console.buf[x + y * Console.Width].Attributes = bkg;
                    Console.buf[x + y * Console.Width].Char.UnicodeChar = ' ';
                }
            }

            // top/bottom bar
            for (int x = left + 1; x <= right - 1; ++x)
            {
                Console.buf[x + top * Console.Width].Char.UnicodeChar = '-';
                Console.buf[x + bottom * Console.Width].Char.UnicodeChar = '-';
            }

            // left/right bar
            for (int y = top + 1; y <= bottom - 1; ++y)
            {
                Console.buf[left + y * Console.Width].Char.UnicodeChar = '|';
                Console.buf[right + y * Console.Width].Char.UnicodeChar = '|';
            }

            // corners
            Console.buf[left + top * Console.Width].Char.UnicodeChar = '+';
            Console.buf[right + top * Console.Width].Char.UnicodeChar = '+';
            Console.buf[left + bottom * Console.Width].Char.UnicodeChar = '+';
            Console.buf[right + bottom * Console.Width].Char.UnicodeChar = '+';
        }

        public void DrawDialog()
        {
            Pos dialog_pos = (Console.Size - dialog_size) / 2;

            DrawWindow(dialog_pos, dialog_size, Console.MakeColor(ConsoleColor.Black, ConsoleColor.Gray));            

            // text
            for(int y=0; y<lines.Count; ++y)
            {
                string s = lines[y].text;
                Pos off = new Pos(dialog_pos.x + 1, dialog_pos.y + 1);

                switch (lines[y].align)
                {
                    case Line.Align.Left:
                    default:
                        break;
                    case Line.Align.Center:
                        off.x += (dialog_size.x - s.Length) / 2;
                        break;
                    case Line.Align.Right:
                        off.x += dialog_size.x - s.Length;
                        break;
                }

                for(int x=0; x<s.Length; ++x)
                    Console.buf[x + off.x + (y + off.y) * Console.Width].Char.UnicodeChar = s[x];
            }
        }

        public void UpdateDialog()
        {
            if (dialog_f != null)
            {
                if (!dialog_f())
                    have_dialog = false;
            }
            else
            {
                ConsoleKeyInfo k = Console.ReadKey();
                if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)
                    have_dialog = false;
            }
        }

        public void AddText(string text)
        {
            foreach(string s in text.SplitText(Console.Width))
            {
                if(texts.Count >= 100)
                    texts.Dequeue();
                texts.Enqueue(s);
            }
        }

        public void AddText(string text, params object[] objects)
        {
            AddText(string.Format(text, objects));
        }

        public void Start()
        {
            Init();
            while(mode != Mode.Exit)
            {
                Draw();
                bool result = false;
                while(!result)
                    result = Update();
            }
        }

        public void Log(string s)
        {
            log.WriteLine("{0}. {1}", DateTime.Now.ToString("HH:mm:ss"), s);
            log.Flush();
        }

        public void Log(string s, params string[] ss)
        {
            Log(string.Format(s, ss));
        }

        void SaveGame()
        {
            string filename = string.Format("{0}.sav", player_name);

            try
            {
                using(BinaryWriter f = new BinaryWriter(File.Open(filename, FileMode.Create)))
                {
                    // header
                    f.Write('H');
                    f.Write('U');
                    f.Write('N');
                    f.Write((byte)0);

                    // set units ids
                    int id = 0;
                    foreach (Unit u in units)
                    {
                        u.id = id;
                        ++id;
                    }

                    // game vars
                    Utils.rnd.Save(f);
                    f.Write(player_name);
                    f.Write(player.id);

                    // units
                    f.Write(units.Count);
                    foreach(Unit u in units)
                        u.Save(f);

                    // map
                    map.Save(f);
                }
            }
            catch(Exception e)
            {
                Log("Failed to save game to file {0}: {1}", filename, e.ToString());
                ShowDialog("Failed to save game.\nCheck log file for details.");
            }

            Log("Game saved to {0}. Closing...", filename);
            Environment.Exit(0);
        }

        bool TryLoad()
        {
            string filename = string.Format("{0}.sav", player_name);

            if(!File.Exists(filename))
                return false;

            try
            {
                using(BinaryReader f = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    // header
                    char[] sign = new char[3];
                    f.Read(sign, 0, 3);
                    if (sign[0] != 'H' || sign[1] != 'U' || sign[2] != 'N')
                        throw new Exception(string.Format("Invalid file signature '{0}{1}{2}'.", sign[0], sign[1], sign[2]));
                    byte ver;
                    f.Read(out ver);
                    if (ver != 0)
                        throw new Exception(string.Format("Invalid file version '{0}'.", ver));

                    // game vars
                    Utils.rnd.Load(f);
                    player_name = f.ReadString();
                    int player_id = f.ReadInt32();

                    // units
                    int count = f.ReadInt32();
                    units.Clear();
                    for(int i=0; i<count; ++i)
                    {
                        Unit u = new Unit();
                        u.Load(f);
                        units.Add(u);
                    }
                    Unit.units = units;

                    // map
                    map.Load(f);

                    if (player_id < 0 || player_id >= units.Count || units[player_id].ai)
                        throw new Exception(string.Format("Invalid player id '{0}'.", player_id));

                    player = units[player_id];
                    throw_prev = null;
                    throw_target = null;
                    mode = Mode.Game;
                    return true;
                }
            }
            catch(Exception e)
            {
                Log("Failed to load game from file {0}: {1}", filename, e.ToString());
                mode = Mode.Invalid;
                ShowDialogAction("Failed to load game.\nCheck log file for details.", () => Environment.Exit(1));
                while(true)
                {
                    Draw();
                    UpdateDialog();
                }
            }
        }
    }
}
