using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Game
    {
        enum Mode
        {
            Game,
            Inventory,
            Exit,
            Look,
            Throw
        }

        enum InventoryAction
        {
            None,
            Look,
            Drop,
            Get,
            Equip,
            Remove,
            Use,
            Throw
        }

        class InvItem
        {
            public Item item;
            public int count, selected_count;
            public int index;
            public bool selected;
        }

        public Map map;
        public List<Unit> units = new List<Unit>();
        public Unit player;
        public Pos screen_size;
        public Queue<string> texts = new Queue<string>();
        Mode mode;
        int inv_offset, inv_selected;
        bool inv_have_number, player_acted, inv_from_game;
        List<InvItem> inv_items = new List<InvItem>();
        InventoryAction inv_action;

        Pos look_pos;
        float look_timer;
        bool look_blink;

        Item throw_prev;
        int throw_index;
        Unit throw_target;

        public void Init()
        {
            Console.Init("Hunters", 70, 26, 20);
            screen_size = new Pos(70, 20);
            map = new Map(100, 100);
            mode = Mode.Game;

            player = new Unit { pos = new Pos(5, 5), ai = false };
            player.weapon = Item.items[0];
            player.armor = Item.items[1];
            player.items.Add(new ItemSlot(Item.items[2], 2));
            player.items.Add(new ItemSlot(Item.items[3], 15));
            Tile t = map[player.pos];
            t.unit = player;
            t.wall = false;
            units = new List<Unit>();
            units.Add(player);

            Unit ai = new Unit { pos = new Pos(20, 10), ai = true };
            t = map[ai.pos];
            t.unit = ai;
            t.wall = false;
            units.Add(ai);

            ai = new Unit { pos = new Pos(10, 20), ai = true };
            t = map[ai.pos];
            t.unit = ai;
            t.wall = false;
            units.Add(ai);

            AddText("Welcome Tomashu! Press ? for controls.");

            // reset on new game/load
            throw_prev = null;
            throw_target = null;
        }

        public bool HandleQuitDialog()
        {
            ConsoleKeyInfo k = Console.ReadKey();
            if (k.Key == ConsoleKey.Y)
            {
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
                int dmg = Utils.r.Next(1, 5) + (a.ai ? 0 : 1);
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

        void ShowInventory(InventoryAction action, bool check_items)
        {
            if(mode == Mode.Inventory)
            {
                if (check_items && inv_items.Count == 0)
                    ShowDialog(NO_ITEMS);
                else
                    inv_action = action;
                inv_from_game = false;
            }
            else
            {
                PopulateInvItems(player.Items);
                if (check_items && inv_items.Count == 0)
                    ShowDialog(NO_ITEMS);
                else
                {
                    mode = Mode.Inventory;
                    inv_offset = 0;
                    inv_action = action;
                    inv_from_game = true;
                }
            }
            inv_selected = -1;
            inv_have_number = false;
        }

        void PopulateInvItems(IEnumerable<IndexedItem<Item>> items)
        {
            inv_items.Clear();
            foreach (var a in items)
                inv_items.Add(new InvItem
                {
                    item = a.item,
                    count = 1,
                    index = a.index,
                    selected = false,
                    selected_count = 0
                });
        }

        void PopulateInvItems(IEnumerable<IndexedItem<ItemSlot>> items)
        {
            inv_items.Clear();
            foreach (var a in items)
                inv_items.Add(new InvItem
                {
                    item = a.item.item,
                    count = a.item.count,
                    index = a.index,
                    selected = false,
                    selected_count = 0
                });
        }

        void ShowInventory2(InventoryAction action)
        {
            switch(action)
            {
                case InventoryAction.Equip:
                    {
                        var to_equip = player.items.GetIndexes().Where(x => x.item.item.IsEquipable);
                        if (!to_equip.Any())
                            ShowDialog("You don't have any items to equip.");
                        else
                        {
                            PopulateInvItems(to_equip);
                            inv_from_game = (mode != Mode.Inventory);
                            mode = Mode.Inventory;
                            inv_action = InventoryAction.Equip;
                            inv_selected = -1;
                        }
                    }
                    break;
                case InventoryAction.Remove:
                    {
                        var to_remove = player.GetEquipped();
                        if (!to_remove.Any())
                            ShowDialog("You don't have any items to remove.");
                        else
                        {
                            PopulateInvItems(to_remove);
                            inv_from_game = (mode != Mode.Inventory);
                            mode = Mode.Inventory;
                            inv_action = InventoryAction.Remove;
                            inv_selected = -1;
                        }
                    }
                    break;
                case InventoryAction.Use:
                    {
                        var to_use = player.Items.Where(x => x.item.item.IsUseable);
                        if(to_use.Any())
                        {
                            PopulateInvItems(to_use);
                            inv_from_game = (mode != Mode.Inventory);
                            mode = Mode.Inventory;
                            inv_action = InventoryAction.Use;
                            inv_selected = -1;
                        }
                        else
                            ShowDialog("You don't have any useable items.");
                    }
                    break;
                case InventoryAction.Throw:
                    {
                        inv_items.Clear();
                        var to_throw = player.Items.Where(x => x.index != Unit.INDEX_ARMOR);
                        if (to_throw.Any())
                        {
                            PopulateInvItems(to_throw);
                            inv_from_game = (mode != Mode.Inventory);
                            mode = Mode.Inventory;
                            inv_action = InventoryAction.Throw;
                            inv_selected = -1;
                        }
                        else
                            ShowDialog("You don't have any throwable items.");
                    }
                    break;
            }
        }

        private readonly string NO_ITEMS = "You don't have any items.";

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

        bool UpdatePlayer()
        {
            Pos? dir = null;
            bool diagonal = true;

            ConsoleKeyInfo k = Console.ReadKey();
            if (k.Key == ConsoleKey.NumPad1)
                dir = new Pos(-1, 1);
            else if (k.Key == ConsoleKey.NumPad2 || k.Key == ConsoleKey.DownArrow)
            {
                dir = new Pos(0, 1);
                diagonal = false;
            }
            else if (k.Key == ConsoleKey.NumPad3)
                dir = new Pos(1, 1);
            else if (k.Key == ConsoleKey.NumPad4 || k.Key == ConsoleKey.LeftArrow)
            {
                dir = new Pos(-1, 0);
                diagonal = false;
            }
            else if (k.Key == ConsoleKey.NumPad6 || k.Key == ConsoleKey.RightArrow)
            {
                dir = new Pos(1, 0);
                diagonal = false;
            }
            else if (k.Key == ConsoleKey.NumPad7)
                dir = new Pos(-1, -1);
            else if (k.Key == ConsoleKey.NumPad8 || k.Key == ConsoleKey.UpArrow)
            {
                dir = new Pos(0, -1);
                diagonal = false;
            }
            else if (k.Key == ConsoleKey.NumPad9)
                dir = new Pos(1, -1);
            else if (k.Key == ConsoleKey.Q && k.Modifiers == ConsoleModifiers.Shift)
            {
                ShowDialog("$amQuit WITHOUT saving?\n(y/n)", HandleQuitDialog);
            }
            else if (k.KeyChar == '?')
            {
                ShowDialog("$amControls\n==========\n$a"
                    + "lNumpad 1-9 - walk around, Numpad 5 - wait, l - look\n"
                    + "t - throw\n"
                    + "i - inventory, e - equip, r - remove, u - use, d - drop, g/G - get\n"
                    + "? - controls, S - save, Q - quit");
            }
            else if (k.Key == ConsoleKey.NumPad5)
            {
                // wait turn
                return true;
            }
            else if (k.KeyChar == 'i')
                ShowInventory(InventoryAction.None, false);
            else if(k.KeyChar == 'd')
                ShowInventory(InventoryAction.Drop, true);
            else if(k.KeyChar == 'g' || k.KeyChar == 'G')
            {
                Tile t = map[player.pos];
                if (t.items == null)
                    ShowDialog("There is nothing on ground.");
                else if(t.items.Count == 1 && k.KeyChar == 'g')
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
                {
                    mode = Mode.Inventory;
                    inv_action = InventoryAction.Get;
                    inv_have_number = false;
                    inv_offset = 0;
                    inv_selected = -1;
                    inv_from_game = true;
                    inv_items.Clear();
                    for (int i = 0; i < t.items.Count; ++i)
                        inv_items.Add(new InvItem { item = t.items[i].item, count = t.items[i].count, index = i,
                            selected = false, selected_count = 0 });                        
                }
            }
            else if(k.KeyChar == 'e')
            {
                // equip item
                ShowInventory2(InventoryAction.Equip);
            }
            else if(k.KeyChar == 'r')
            {
                // remove item
                ShowInventory2(InventoryAction.Remove);
            }
            else if(k.KeyChar == 'u')
            {
                // use item
                ShowInventory2(InventoryAction.Use);
            }
            else if(k.KeyChar == 'l')
            {
                // look around
                mode = Mode.Look;
                look_pos = player.pos;
                look_timer = 0;
                look_blink = true;
            }
            else if(k.KeyChar == 't')
            {
                // throw, use last throwable
                if(throw_prev != null)
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
                    ShowInventory2(InventoryAction.Throw);
            }
            else if(k.KeyChar == 'T')
            {
                // throw, pick what to throw
                ShowInventory2(InventoryAction.Throw);
            }

            if (dir != null)
            {
                Pos new_pos = player.pos + dir.Value;

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
                    else if (map.CanMove(player.pos, new_pos, diagonal))
                    {
                        t.unit = player;
                        map[player.pos].unit = null;
                        player.pos = new_pos;
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
                }
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
            Dir9
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
                            bool first = (Utils.r.Next(2) == 0);
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
                            ShowDialogAction("$amR. I. P.\nTomashu 1990 - 2015", () => { mode = Mode.Exit; });
                    }
                }
                else if (mode == Mode.Inventory)
                {
                    int lines = Math.Min(inv_items.Count, Console.Height - 6);
                    ConsoleKeyInfo k = Console.ReadKey();
                    if (k.Key == ConsoleKey.NumPad2 || k.Key == ConsoleKey.DownArrow)
                    {
                        ++inv_offset;
                        if (inv_offset > inv_items.Count - lines)
                            inv_offset = inv_items.Count - lines;
                    }
                    else if (k.Key == ConsoleKey.NumPad8 || k.Key == ConsoleKey.UpArrow)
                    {
                        if (inv_offset > 0)
                            --inv_offset;
                    }
                    else if (k.Key == ConsoleKey.NumPad4 || k.Key == ConsoleKey.LeftArrow)
                    {
                        inv_offset -= lines;
                        if (inv_offset < 0)
                            inv_offset = 0;
                    }
                    else if (k.Key == ConsoleKey.NumPad6 || k.Key == ConsoleKey.RightArrow)
                    {
                        inv_offset += lines;
                        if (inv_offset > inv_items.Count - lines)
                            inv_offset = inv_items.Count - lines;
                    }
                    else if (k.KeyChar == '?')
                    {
                        ShowDialog("$amInventory controls\nArrows/Numpad - navigate\nl - look at item\ne - equip item\n"
                            + "r - remove item\nu - use item\nd - drop item\nt - throw item\nEscape - close");
                    }
                    if (inv_action == InventoryAction.None)
                    {
                        if (k.Key == ConsoleKey.Escape)
                            mode = Mode.Game;
                        else if (k.KeyChar == 'l')
                            ShowInventory(InventoryAction.Look, true);
                        else if (k.KeyChar == 'd')
                            ShowInventory(InventoryAction.Drop, true);
                        else if (k.KeyChar == 'e')
                            ShowInventory2(InventoryAction.Equip);
                        else if (k.KeyChar == 'r')
                            ShowInventory2(InventoryAction.Remove);
                        else if (k.KeyChar == 'u')
                            ShowInventory2(InventoryAction.Use);
                        else if (k.KeyChar == 't')
                            ShowInventory2(InventoryAction.Throw);
                    }
                    else
                    {
                        if (k.KeyChar == '?')
                        {
                            string verb;
                            bool multi;

                            switch (inv_action)
                            {
                                default:
                                case InventoryAction.Look:
                                    verb = "examine";
                                    multi = false;
                                    break;
                                case InventoryAction.Drop:
                                    verb = "drop";
                                    multi = true;
                                    break;
                                case InventoryAction.Get:
                                    verb = "get";
                                    multi = true;
                                    break;
                                case InventoryAction.Equip:
                                    verb = "equip";
                                    multi = false;
                                    break;
                                case InventoryAction.Remove:
                                    verb = "remove";
                                    multi = false;
                                    break;
                                case InventoryAction.Use:
                                    verb = "use";
                                    multi = false;
                                    break;
                                case InventoryAction.Throw:
                                    verb = "throw";
                                    multi = false;
                                    break;
                            }

                            string text;
                            if(multi)
                                text = "$am{0} controls\nPress item index to select it.\nNumbers - pick count\n"
                                    + "Backspace/delete - alter count\nArrows/numpad - navigate\n"
                                    + "Enter - {1} items\nEscape - exit";
                            else
                                text = "$am{0} controls\nPress item index to {1} it.\nArrows/numpad - navigate\n"
                                        + "Escape - exit";
                            
                            ShowDialog(string.Format(text, verb.Up(), verb));
                        }
                        else if (k.Key == ConsoleKey.Escape)
                        {
                            if (inv_from_game)
                                mode = Mode.Game;
                            else
                            {
                                if (inv_action == InventoryAction.Drop)
                                {
                                    for (int i = 0; i < inv_items.Count; ++i)
                                    {
                                        inv_items[i].selected = false;
                                        inv_items[i].selected_count = 0;
                                    }
                                }
                                inv_action = InventoryAction.None;
                            }
                        }
                        else if (k.Key == ConsoleKey.Enter)
                        {
                            if (inv_action == InventoryAction.Drop)
                            {
                                var to_drop = inv_items.Where(x => x.selected_count > 0);
                                if (to_drop.Any())
                                {
                                    Tile t = map[player.pos];
                                    if (t.items == null)
                                        t.items = new List<ItemSlot>();

                                    StringBuilder s = new StringBuilder("You dropped ");
                                    s.Join(to_drop.Select(x => (x.selected_count > 1 ? string.Format("{0} {1}s", x.selected_count, x.item.name) : x.item.name)).ToList());
                                    s.Append(" on ground.");
                                    AddText(s.ToString());

                                    foreach (var item in to_drop)
                                    {
                                        t.AddItem(item.item, item.selected_count);
                                        if (item.index >= 0)
                                            player.items[item.index].count -= item.selected_count;
                                        else
                                            player.RemoveEquipped(item.index);
                                    }

                                    player.items.RemoveAll(x => x.count == 0);
                                    mode = Mode.Game;
                                    player_acted = true;
                                }
                            }
                            else if (inv_action == InventoryAction.Get)
                            {
                                var to_get = inv_items.Where(x => x.selected_count > 0);
                                if (to_get.Any())
                                {
                                    Tile t = map[player.pos];

                                    StringBuilder s = new StringBuilder("You picked ");
                                    s.Join(to_get.Select(x => (x.selected_count > 1 ? string.Format("{0} {1}s", x.selected_count, x.item.name) : x.item.name)).ToList());
                                    s.Append(" from ground.");
                                    AddText(s.ToString());

                                    foreach (var item in to_get)
                                    {
                                        player.AddItem(new ItemSlot(item.item, item.selected_count));
                                        t.items[item.index].count -= item.selected_count;
                                    }

                                    t.items.RemoveAll(x => x.count == 0);
                                    if (t.items.Count == 0)
                                        t.items = null;
                                    mode = Mode.Game;
                                    player_acted = true;
                                }
                            }
                        }
                        else if (k.KeyChar >= 'a' && k.KeyChar <= 'z')
                        {
                            int index = k.KeyChar - 'a';
                            int found_index = -1;

                            for (int i = 0; i < Math.Min(inv_items.Count, lines); ++i)
                            {
                                int item_index = (i + inv_offset) % 26;
                                if (index == item_index)
                                {
                                    found_index = i + inv_offset;
                                    break;
                                }
                            }

                            if (found_index != -1)
                            {
                                InvItem item = inv_items[found_index];
                                if (inv_action == InventoryAction.Look)
                                {
                                    ShowDialog(string.Format("You examine {0}. It looks normal.", item.item.name));
                                }
                                else if (inv_action == InventoryAction.Get || inv_action == InventoryAction.Drop)
                                {
                                    if (item.selected_count == 0)
                                        item.selected_count = item.count;
                                    else
                                        item.selected_count = 0;
                                    if (inv_selected != found_index)
                                    {
                                        if (inv_selected != -1)
                                            inv_items[inv_selected].selected = false;
                                        item.selected = true;
                                        inv_selected = found_index;
                                    }
                                }
                                else if (inv_action == InventoryAction.Equip)
                                {
                                    // equip item
                                    int item_index = Unit.GetIndex(item.item);
                                    Item prev_item = player.GetEquipped(item_index);
                                    player.Equip(item.item);
                                    string text;
                                    if (prev_item != null)
                                    {
                                        player.AddItem(new ItemSlot(prev_item, 1));
                                        if (item.item.type == Item.Type.Weapon)
                                            text = "You hide {0} and start using {1}.";
                                        else
                                            text = "You remove {0} and wear {1}.";
                                        text = string.Format(text, prev_item.name, item.item.name);
                                    }
                                    else
                                    {
                                        if (item.item.type == Item.Type.Weapon)
                                            text = "You start using {0}.";
                                        else
                                            text = "You wear {0}.";
                                        text = string.Format(text, item.item.name);
                                    }
                                    AddText(text);
                                    player.items.RemoveAt(item.index);
                                    mode = Mode.Game;
                                    player_acted = true;

                                }
                                else if (inv_action == InventoryAction.Remove)
                                {
                                    // remove equipped item
                                    player.RemoveEquipped(item.index);
                                    player.AddItem(new ItemSlot(item.item, 1));
                                    string text;
                                    if (item.item.type == Item.Type.Weapon)
                                        text = "You hide {0}.";
                                    else
                                        text = "You remove {0}.";
                                    AddText(text, item.item.name);
                                    mode = Mode.Game;
                                    player_acted = true;
                                }
                                else if (inv_action == InventoryAction.Use)
                                {
                                    // use item
                                    player.hp += 5;
                                    if (player.hpmax > 10)
                                        player.hpmax = 10;
                                    AddText("You drink {0}, you are healed.", item.item.name);
                                    player.items[item.index].count--;
                                    if (player.items[item.index].count == 0)
                                        player.items.RemoveAt(item.index);
                                    mode = Mode.Game;
                                    player_acted = true;
                                }
                                else if(inv_action == InventoryAction.Throw)
                                {
                                    // throw item
                                    mode = Mode.Throw;
                                    throw_index = item.index;
                                    throw_prev = item.item;
                                    PickThrowTarget();
                                }
                            }
                        }
                        else if (k.KeyChar >= '0' && k.KeyChar <= '9')
                        {
                            if ((inv_action == InventoryAction.Drop || inv_action == InventoryAction.Get) && inv_selected != -1)
                            {
                                InvItem item = inv_items[inv_selected];
                                int num = k.KeyChar - '0';
                                if (!inv_have_number)
                                    item.selected_count = 0;
                                item.selected_count *= 10;
                                item.selected_count += num;
                                if (item.selected_count > item.count)
                                {
                                    item.selected_count = item.count;
                                    inv_have_number = false;
                                }
                                else
                                    inv_have_number = true;
                            }
                        }
                        else if (k.Key == ConsoleKey.Backspace)
                        {
                            if ((inv_action == InventoryAction.Drop || inv_action == InventoryAction.Get) && inv_selected != -1)
                            {
                                InvItem item = inv_items[inv_selected];
                                item.selected_count /= 10;
                                if (item.selected_count == 0)
                                    inv_have_number = false;
                            }
                        }
                        else if (k.Key == ConsoleKey.Delete)
                        {
                            if ((inv_action == InventoryAction.Drop || inv_action == InventoryAction.Get) && inv_selected != -1)
                            {
                                InvItem item = inv_items[inv_selected];
                                item.selected_count = 0;
                                inv_have_number = false;
                            }
                        }
                        else if (k.KeyChar == '*')
                        {
                            if (inv_action == InventoryAction.Drop || inv_action == InventoryAction.Get)
                            {
                                if (inv_items.All(x => x.selected_count == x.count))
                                {
                                    // all selected, deselect
                                    inv_items.ForEach(x => x.selected_count = 0);
                                }
                                else
                                {
                                    // all not selected, select
                                    inv_items.ForEach(x => x.selected_count = x.count);
                                }
                            }
                        }
                    }
                }
                else if (mode == Mode.Look || mode == Mode.Throw)
                    return UpdateLook(dt);
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

            Dir? dir = null;
            ConsoleKeyInfo k = ka.Value;

            if (k.KeyChar == '?')
            {
                ShowDialog("$amLook controls\nArrows/numpad - navigate\nl - examine tile\nc - center on yourself\n"
                    + "Escape - exit");
            }
            else if (k.KeyChar == 'l')
            {
                ShowDialog("This tile is very interesting!");
            }
            else if (k.KeyChar == 'c')
            {
                look_pos = player.pos;
            }
            else if (k.Key == ConsoleKey.Escape)
            {
                mode = Mode.Game;
            }
            else if (k.Key == ConsoleKey.NumPad1)
                dir = Dir.Dir1;
            else if (k.Key == ConsoleKey.NumPad2 || k.Key == ConsoleKey.DownArrow)
                dir = Dir.Dir2;
            else if (k.Key == ConsoleKey.NumPad3)
                dir = Dir.Dir3;
            else if (k.Key == ConsoleKey.NumPad4 || k.Key == ConsoleKey.LeftArrow)
                dir = Dir.Dir4;
            else if (k.Key == ConsoleKey.NumPad6 || k.Key == ConsoleKey.RightArrow)
                dir = Dir.Dir6;
            else if (k.Key == ConsoleKey.NumPad7)
                dir = Dir.Dir7;
            else if (k.Key == ConsoleKey.NumPad8 || k.Key == ConsoleKey.UpArrow)
                dir = Dir.Dir8;
            else if (k.Key == ConsoleKey.NumPad9)
                dir = Dir.Dir9;
            else
                return blink;

            if (dir != null)
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

            return true;
        }

        public void Draw()
        {
            Console.Clear();

            Pos my_pos = ((mode == Mode.Look || mode == Mode.Throw) ? look_pos : player.pos);
            Pos offset = new Pos(my_pos.x - screen_size.x / 2, my_pos.y - screen_size.y / 2);
            offset.y -= 1;

            short bkg = (((short)ConsoleColor.DarkGray) << 4) | ((short)ConsoleColor.Black);
            string text = string.Format("Tomashu L:1 XP:0% HP:{0}/{1} W:{2} A:{3}",
                player.hp,
                player.hpmax,
                player.weapon != null ? player.weapon.name : "fists",
                player.armor != null ? player.armor.name : "clothes"); 

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

            // units
            int left = Math.Max(0, offset.x);
            int right = Math.Min(map.w, offset.x + screen_size.x);
            int top = Math.Max(0, offset.y);
            int bottom = Math.Min(map.h, offset.y + screen_size.y);

            offset.y -= 1;

            foreach (Unit u in units)
            {
                if(u.pos.x >= left && u.pos.x < right && u.pos.y >= top && u.pos.y < bottom)
                    Console.buf[u.pos.x - offset.x + (u.pos.y - offset.y) * Console.Width].Char.UnicodeChar = '@';
            }

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
                        else if (map[pt].wall || map[pt].unit != null)
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

            if(mode == Mode.Inventory)
            {
                int w = 50;
                int h = Math.Min(Console.Height - 5, inv_items.Count+1);
                int lines = Math.Min(inv_items.Count, Console.Height - 6);

                Pos size = new Pos(w,h);
                Pos pos = (Console.Size - size) / 2;

                string inv_text;
                switch(inv_action)
                {
                    case InventoryAction.None:
                    default:
                        inv_text = "INVENTORY";
                        break;
                    case InventoryAction.Look:
                        inv_text = "EXAMINE ITEMS";
                        break;
                    case InventoryAction.Drop:
                        inv_text = "DROP ITEMS";
                        break;
                    case InventoryAction.Get:
                        inv_text = "PICKUP ITEMS";
                        break;
                    case InventoryAction.Equip:
                        inv_text = "EQUIP ITEM";
                        break;
                    case InventoryAction.Remove:
                        inv_text = "REMOVE ITEM";
                        break;
                    case InventoryAction.Use:
                        inv_text = "USE ITEM";
                        break;
                    case InventoryAction.Throw:
                        inv_text = "THROW ITEM";
                        break;
                }

                DrawWindow(pos, size, Console.MakeColor(ConsoleColor.Black, have_dialog ? ConsoleColor.DarkGray : ConsoleColor.Gray));

                if (inv_offset > 0)
                    Console.buf[pos.x + size.x - 1 + (pos.y + 1) * Console.Width].Char.UnicodeChar = '^';

                if (inv_offset != inv_items.Count - lines)
                    Console.buf[pos.x + size.x - 1 + (pos.y + size.y - 1) * Console.Width].Char.UnicodeChar = 'v';

                Console.WriteText(inv_text, new Pos(pos.x + (size.x - inv_text.Length) / 2, pos.y));

                StringBuilder s = new StringBuilder();
                short selected_bkg = Console.MakeColor(ConsoleColor.Black, ConsoleColor.White);

                for(int i=0; i<Math.Min(inv_items.Count, lines); ++i)
                {
                    InvItem item = inv_items[i + inv_offset];
                    if(item.selected)
                    {
                        for(int x=0; x<size.x-1; ++x)
                            Console.buf[pos.x+1+x + (pos.y+i+1) * Console.Width].Attributes = selected_bkg;
                    }
                    s.Clear();
                    if(inv_action == InventoryAction.Drop || inv_action == InventoryAction.Get)
                    {
                        if(item.selected_count > 0)
                            s.Append("+ ");
                        else
                            s.Append("- ");
                    }
                    if (item.index < 0)
                        s.Append("*");
                    s.Append(string.Format("{0}. {1}", (char)('a' + (i+inv_offset)%26), item.item));
                    if(item.count > 1)
                        s.Append(string.Format(" x{0}", item.count));

                    Console.WriteText(s.ToString(), new Pos(pos.x + 1, pos.y + i + 1));

                    if(item.selected_count > 0)
                    {
                        string st = string.Format("{0}/{1}", item.selected_count, item.count);
                        Console.WriteText(st, new Pos(pos.x + w - st.Length, pos.y + i + 1));
                    }
                }

                if(inv_action != InventoryAction.None)
                {
                    string action_text;
                    switch(inv_action)
                    {
                        case InventoryAction.Look:
                            action_text = "Pick item to examine.";
                            break;
                        case InventoryAction.Drop:
                            action_text = "Pick items to drop.";
                            break;
                        case InventoryAction.Get:
                            action_text = "Pick items to get.";
                            break;
                        case InventoryAction.Equip:
                            action_text = "Pick item to equip.";
                            break;
                        case InventoryAction.Remove:
                            action_text = "Pick item to remove.";
                            break;
                        case InventoryAction.Use:
                            action_text = "Pick item to use.";
                            break;
                        case InventoryAction.Throw:
                            action_text = "Pick item to throw.";
                            break;
                        default:
                            action_text = "!MISS!";
                            break;
                    }
                        
                    Console.WriteText(action_text, new Pos(pos.x + (size.x - action_text.Length) / 2, Console.Height - 1),
                        Console.MakeColor(ConsoleColor.Black, ConsoleColor.White));
                }
            }

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

        bool have_dialog;
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

        void DrawWindow(Pos pos, Pos size, short bkg)
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
    }
}
