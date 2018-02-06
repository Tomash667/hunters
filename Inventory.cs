using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Inventory
    {
        public enum Action
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
            public GameItem game_item;
            public int selected_count, index;
            public bool selected;
        }

        int offset, selected;
        bool have_number, from_game;
        List<InvItem> items = new List<InvItem>();
        Action action;

        public void Show(Action new_action)
        {
            switch (new_action)
            {
                case Action.None:
                case Action.Drop:
                case Action.Look:
                    {
                        PopulateInvItems(Game.Instance.player.Items);
                        if (items.Count == 0)
                            Game.Instance.ShowDialog("You don't have any items.");
                        else
                        {
                            from_game = Game.Instance.ChangeMode(Game.Mode.Inventory);
                            action = new_action;
                            offset = 0;
                            selected = -1;
                            have_number = false;
                        }
                    }
                    break;
                case Action.Equip:
                    {
                        var to_equip = Game.Instance.player.items.GetIndexes().Where(x => x.item.item.IsEquipable);
                        if (!to_equip.Any())
                            Game.Instance.ShowDialog("You don't have any items to equip.");
                        else
                        {
                            PopulateInvItems(to_equip);
                            from_game = Game.Instance.ChangeMode(Game.Mode.Inventory);
                            action = Action.Equip;
                            selected = -1;
                        }
                    }
                    break;
                case Action.Remove:
                    {
                        var to_remove = Game.Instance.player.GetEquipped();
                        if (!to_remove.Any())
                            Game.Instance.ShowDialog("You don't have any items to remove.");
                        else
                        {
                            PopulateInvItems(to_remove);
                            from_game = Game.Instance.ChangeMode(Game.Mode.Inventory);
                            action = Action.Remove;
                            selected = -1;
                        }
                    }
                    break;
                case Action.Use:
                    {
                        var to_use = Game.Instance.player.Items.Where(x => x.item.item.IsUseable);
                        if (to_use.Any())
                        {
                            PopulateInvItems(to_use);
                            from_game = Game.Instance.ChangeMode(Game.Mode.Inventory);
                            action = Action.Use;
                            selected = -1;
                        }
                        else
                            Game.Instance.ShowDialog("You don't have any useable items.");
                    }
                    break;
                case Action.Throw:
                    {
                        items.Clear();
                        var to_throw = Game.Instance.player.Items.Where(x => x.index != Unit.INDEX_ARMOR);
                        if (to_throw.Any())
                        {
                            PopulateInvItems(to_throw);
                            from_game = Game.Instance.ChangeMode(Game.Mode.Inventory);
                            action = Action.Throw;
                            selected = -1;
                        }
                        else
                            Game.Instance.ShowDialog("You don't have any throwable items.");
                    }
                    break;
            }
        }

        public void ShowGet(List<GameItem> _items)
        {
            Game.Instance.mode = Game.Mode.Inventory;
            action = Action.Get;
            have_number = false;
            offset = 0;
            selected = -1;
            from_game = true;
            items.Clear();
            for (int i = 0; i < _items.Count; ++i)
                items.Add(new InvItem
                {
                    game_item = _items[i],
                    index = i,
                    selected = false,
                    selected_count = 0
                });
        }

        void PopulateInvItems(IEnumerable<IndexedItem<GameItem>> _items)
        {
            items.Clear();
            foreach (var a in _items)
                items.Add(new InvItem
                {
                    game_item = a.item,
                    index = a.index,
                    selected = false,
                    selected_count = 0
                });
        }

        public void Update()
        {
            int lines = Math.Min(items.Count, Console.Height - 6);
            ConsoleKeyInfo k = Console.ReadKey();
            if (k.Key == ConsoleKey.NumPad2 || k.Key == ConsoleKey.DownArrow)
            {
                ++offset;
                if (offset > items.Count - lines)
                    offset = items.Count - lines;
            }
            else if (k.Key == ConsoleKey.NumPad8 || k.Key == ConsoleKey.UpArrow)
            {
                if (offset > 0)
                    --offset;
            }
            else if (k.Key == ConsoleKey.NumPad4 || k.Key == ConsoleKey.LeftArrow)
            {
                offset -= lines;
                if (offset < 0)
                    offset = 0;
            }
            else if (k.Key == ConsoleKey.NumPad6 || k.Key == ConsoleKey.RightArrow)
            {
                offset += lines;
                if (offset > items.Count - lines)
                    offset = items.Count - lines;
            }
            else if (k.KeyChar == '?')
            {
                Game.Instance.ShowDialog("$amInventory controls\nArrows/Numpad - navigate\nl - look at item\ne - equip item\n"
                    + "r - remove item\nu - use item\nd - drop item\nt - throw item\nEscape - close");
            }
            if (action == Action.None)
            {
                if (k.Key == ConsoleKey.Escape)
                    Game.Instance.mode = Game.Mode.Game;
                else if (k.KeyChar == 'l')
                    Show(Action.Look);
                else if (k.KeyChar == 'd')
                    Show(Action.Drop);
                else if (k.KeyChar == 'e')
                    Show(Action.Equip);
                else if (k.KeyChar == 'r')
                    Show(Action.Remove);
                else if (k.KeyChar == 'u')
                    Show(Action.Use);
                else if (k.KeyChar == 't')
                    Show(Action.Throw);
            }
            else
            {
                if (k.KeyChar == '?')
                {
                    string verb;
                    bool multi;

                    switch (action)
                    {
                        default:
                        case Action.Look:
                            verb = "examine";
                            multi = false;
                            break;
                        case Action.Drop:
                            verb = "drop";
                            multi = true;
                            break;
                        case Action.Get:
                            verb = "get";
                            multi = true;
                            break;
                        case Action.Equip:
                            verb = "equip";
                            multi = false;
                            break;
                        case Action.Remove:
                            verb = "remove";
                            multi = false;
                            break;
                        case Action.Use:
                            verb = "use";
                            multi = false;
                            break;
                        case Action.Throw:
                            verb = "throw";
                            multi = false;
                            break;
                    }

                    string text;
                    if (multi)
                        text = "$am{0} controls\nPress item index to select it.\nNumbers - pick count\n"
                            + "Backspace/delete - alter count\nArrows/numpad - navigate\n"
                            + "Enter - {1} items\nEscape - exit";
                    else
                        text = "$am{0} controls\nPress item index to {1} it.\nArrows/numpad - navigate\n"
                                + "Escape - exit";

                    Game.Instance.ShowDialog(string.Format(text, verb.Up(), verb));
                }
                else if (k.Key == ConsoleKey.Escape)
                {
                    if (from_game)
                        Game.Instance.mode = Game.Mode.Game;
                    else
                    {
                        if (action == Action.Drop)
                        {
                            for (int i = 0; i < items.Count; ++i)
                            {
                                items[i].selected = false;
                                items[i].selected_count = 0;
                            }
                        }
                        action = Action.None;
                    }
                }
                else if (k.Key == ConsoleKey.Enter)
                {
                    if (action == Action.Drop)
                    {
                        var to_drop = items.Where(x => x.selected_count > 0);
                        if (to_drop.Any())
                        {
                            Tile t = Game.Instance.map[Game.Instance.player.pos];
                            if (t.items == null)
                                t.items = new List<GameItem>();

                            StringBuilder s = new StringBuilder("You dropped ");
                            s.Join(to_drop.Select(x =>
                                (x.selected_count > 1 ? string.Format("{0} {1}s", x.selected_count, x.game_item.item.name)
                                : x.game_item.item.name)).ToList());
                            s.Append(" on ground.");
                            Game.Instance.AddText(s.ToString());

                            foreach (var inv_item in to_drop)
                            {
                                t.AddItem(inv_item.game_item.item, inv_item.selected_count, inv_item.game_item.ammo);
                                if (inv_item.index >= 0)
                                    Game.Instance.player.items[inv_item.index].count -= inv_item.selected_count;
                                else
                                    Game.Instance.player.RemoveEquipped(inv_item.index);
                            }

                            Game.Instance.player.items.RemoveAll(x => x.count == 0);
                            Game.Instance.mode = Game.Mode.Game;
                            Game.Instance.player_acted = true;
                        }
                    }
                    else if (action == Action.Get)
                    {
                        var to_get = items.Where(x => x.selected_count > 0);
                        if (to_get.Any())
                        {
                            Tile t = Game.Instance.map[Game.Instance.player.pos];

                            StringBuilder s = new StringBuilder("You picked ");
                            s.Join(to_get.Select(x => (x.selected_count > 1 ?
                                string.Format("{0} {1}s", x.selected_count, x.game_item.item.name) : x.game_item.item.name)).ToList());
                            s.Append(" from ground.");
                            Game.Instance.AddText(s.ToString());

                            foreach (var inv_item in to_get)
                            {
                                Game.Instance.player.AddItem(new GameItem(inv_item.game_item.item, inv_item.selected_count,
                                    inv_item.game_item.ammo));
                                t.items[inv_item.index].count -= inv_item.selected_count;
                            }

                            t.items.RemoveAll(x => x.count == 0);
                            if (t.items.Count == 0)
                                t.items = null;
                            Game.Instance.mode = Game.Mode.Game;
                            Game.Instance.player_acted = true;
                        }
                    }
                }
                else if (k.KeyChar >= 'a' && k.KeyChar <= 'z')
                {
                    int index = k.KeyChar - 'a';
                    int found_index = -1;

                    for (int i = 0; i < Math.Min(items.Count, lines); ++i)
                    {
                        int item_index = (i + offset) % 26;
                        if (index == item_index)
                        {
                            found_index = i + offset;
                            break;
                        }
                    }

                    if (found_index != -1)
                    {
                        InvItem inv_item = items[found_index];
                        if (action == Action.Look)
                            Game.Instance.ShowDialog(string.Format("You examine {0}. It looks normal.", inv_item.game_item.item.name));
                        else if (action == Action.Get || action == Action.Drop)
                        {
                            if (inv_item.selected_count == 0)
                                inv_item.selected_count = inv_item.game_item.count;
                            else
                                inv_item.selected_count = 0;
                            if (selected != found_index)
                            {
                                if (selected != -1)
                                    items[selected].selected = false;
                                inv_item.selected = true;
                                selected = found_index;
                            }
                        }
                        else if (action == Action.Equip)
                        {
                            // equip item
                            int item_index = Unit.GetIndex(inv_item.game_item.item.type);
                            GameItem prev_item = Game.Instance.player.GetEquipped(item_index);
                            Game.Instance.player.Equip(inv_item.game_item);
                            string text;
                            if (prev_item != null)
                            {
                                Game.Instance.player.AddItem(prev_item);
                                if (inv_item.game_item.item.type == Item.Type.Weapon)
                                    text = "You hide {0} and start using {1}.";
                                else
                                    text = "You remove {0} and wear {1}.";
                                text = string.Format(text, prev_item.item.name, inv_item.game_item.item.name);
                            }
                            else
                            {
                                if (inv_item.game_item.item.type == Item.Type.Weapon)
                                    text = "You start using {0}.";
                                else
                                    text = "You wear {0}.";
                                text = string.Format(text, inv_item.game_item.item.name);
                            }
                            Game.Instance.AddText(text);
                            Game.Instance.player.items.RemoveAt(inv_item.index);
                            Game.Instance.mode = Game.Mode.Game;
                            Game.Instance.player_acted = true;
                        }
                        else if (action == Action.Remove)
                        {
                            // remove equipped item
                            Game.Instance.player.RemoveEquipped(inv_item.index);
                            Game.Instance.player.AddItem(new GameItem(inv_item.game_item.item, 1));
                            string text;
                            if (inv_item.game_item.item.type == Item.Type.Weapon)
                                text = "You hide {0}.";
                            else
                                text = "You remove {0}.";
                            Game.Instance.AddText(text, inv_item.game_item.item.name);
                            Game.Instance.mode = Game.Mode.Game;
                            Game.Instance.player_acted = true;
                        }
                        else if (action == Action.Use)
                        {
                            // use item
                            Game.Instance.player.hp = Math.Max(Game.Instance.player.hp + 5, Game.Instance.player.hpmax);
                            Game.Instance.AddText("You drink {0}, you are healed.", inv_item.game_item.item.name);
                            Game.Instance.player.items[inv_item.index].count--;
                            if (Game.Instance.player.items[inv_item.index].count == 0)
                                Game.Instance.player.items.RemoveAt(inv_item.index);
                            Game.Instance.mode = Game.Mode.Game;
                            Game.Instance.player_acted = true;
                        }
                        else if (action == Action.Throw)
                        {
                            // throw item
                            Game.Instance.StartThrow(inv_item.index, inv_item.game_item.item);
                        }
                    }
                }
                else if (k.KeyChar >= '0' && k.KeyChar <= '9')
                {
                    if ((action == Action.Drop || action == Action.Get) && selected != -1)
                    {
                        InvItem inv_item = items[selected];
                        int num = k.KeyChar - '0';
                        if (!have_number)
                            inv_item.selected_count = 0;
                        inv_item.selected_count *= 10;
                        inv_item.selected_count += num;
                        if (inv_item.selected_count > inv_item.game_item.count)
                        {
                            inv_item.selected_count = inv_item.game_item.count;
                            have_number = false;
                        }
                        else
                            have_number = true;
                    }
                }
                else if (k.Key == ConsoleKey.Backspace)
                {
                    if ((action == Action.Drop || action == Action.Get) && selected != -1)
                    {
                        InvItem inv_item = items[selected];
                        inv_item.selected_count /= 10;
                        if (inv_item.selected_count == 0)
                            have_number = false;
                    }
                }
                else if (k.Key == ConsoleKey.Delete)
                {
                    if ((action == Action.Drop || action == Action.Get) && selected != -1)
                    {
                        InvItem inv_item = items[selected];
                        inv_item.selected_count = 0;
                        have_number = false;
                    }
                }
                else if (k.KeyChar == '*')
                {
                    if (action == Action.Drop || action == Action.Get)
                    {
                        if (items.All(x => x.selected_count == x.game_item.count))
                        {
                            // all selected, deselect
                            items.ForEach(x => x.selected_count = 0);
                        }
                        else
                        {
                            // all not selected, select
                            items.ForEach(x => x.selected_count = x.game_item.count);
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            int w = 50;
            int h = Math.Min(Console.Height - 5, items.Count + 1);
            int lines = Math.Min(items.Count, Console.Height - 6);

            Pos size = new Pos(w, h);
            Pos pos = (Console.Size - size) / 2;

            string inv_text;
            switch (action)
            {
                case Action.None:
                default:
                    inv_text = "INVENTORY";
                    break;
                case Action.Look:
                    inv_text = "EXAMINE ITEMS";
                    break;
                case Action.Drop:
                    inv_text = "DROP ITEMS";
                    break;
                case Action.Get:
                    inv_text = "PICKUP ITEMS";
                    break;
                case Action.Equip:
                    inv_text = "EQUIP ITEM";
                    break;
                case Action.Remove:
                    inv_text = "REMOVE ITEM";
                    break;
                case Action.Use:
                    inv_text = "USE ITEM";
                    break;
                case Action.Throw:
                    inv_text = "THROW ITEM";
                    break;
            }

            Game.Instance.DrawWindow(pos, size, Console.MakeColor(ConsoleColor.Black,
                Game.Instance.have_dialog ? ConsoleColor.DarkGray : ConsoleColor.Gray));

            if (offset > 0)
                Console.buf[pos.x + size.x - 1 + (pos.y + 1) * Console.Width].Char.UnicodeChar = '^';

            if (offset != items.Count - lines)
                Console.buf[pos.x + size.x - 1 + (pos.y + size.y - 1) * Console.Width].Char.UnicodeChar = 'v';

            Console.WriteText(inv_text, new Pos(pos.x + (size.x - inv_text.Length) / 2, pos.y));

            StringBuilder s = new StringBuilder();
            short selected_bkg = Console.MakeColor(ConsoleColor.Black, ConsoleColor.White);

            for (int i = 0; i < Math.Min(items.Count, lines); ++i)
            {
                InvItem inv_item = items[i + offset];
                if (inv_item.selected)
                {
                    for (int x = 0; x < size.x - 1; ++x)
                        Console.buf[pos.x + 1 + x + (pos.y + i + 1) * Console.Width].Attributes = selected_bkg;
                }
                s.Clear();
                if (action == Action.Drop || action == Action.Get)
                {
                    if (inv_item.selected_count > 0)
                        s.Append("+ ");
                    else
                        s.Append("- ");
                }
                if (inv_item.index < 0)
                    s.Append("*");
                s.Append(string.Format("{0}. {1}", (char)('a' + (i + offset) % 26), inv_item.game_item));
                if (inv_item.game_item.count > 1)
                    s.Append(string.Format(" x{0}", inv_item.game_item.count));

                Console.WriteText(s.ToString(), new Pos(pos.x + 1, pos.y + i + 1));

                if (inv_item.selected_count > 0)
                {
                    string st = string.Format("{0}/{1}", inv_item.selected_count, inv_item.game_item.count);
                    Console.WriteText(st, new Pos(pos.x + w - st.Length, pos.y + i + 1));
                }
            }

            if (action != Action.None)
            {
                string action_text;
                switch (action)
                {
                    case Action.Look:
                        action_text = "Pick item to examine.";
                        break;
                    case Action.Drop:
                        action_text = "Pick items to drop.";
                        break;
                    case Action.Get:
                        action_text = "Pick items to get.";
                        break;
                    case Action.Equip:
                        action_text = "Pick item to equip.";
                        break;
                    case Action.Remove:
                        action_text = "Pick item to remove.";
                        break;
                    case Action.Use:
                        action_text = "Pick item to use.";
                        break;
                    case Action.Throw:
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
    }
}
