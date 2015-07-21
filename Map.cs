using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Map
    {
        public int w, h;
        public List<Tile> m;

        public Map(int _w, int _h)
        {
            w = _w;
            h = _h;
            m = new List<Tile>(w * h);
            for(int i=0; i<w*h; ++i)
            {
                m.Add(new Tile());
            }
        }

        public Tile this [int x, int y]
        {
            get
            {
                return m[x + y * w];
            }
        }

        public Tile this [Pos pos]
        {
            get
            {
                return m[pos.x + pos.y * w];
            }
        }

        public void Draw(Pos size, Pos offset, Pos buf_offset)
        {
            int left = Math.Max(0, offset.x);
            int right = Math.Min(w, offset.x + size.x);
            int top = Math.Max(0, offset.y);
            int bottom = Math.Min(h, offset.y + size.y);
            bool lit;
            char glyph;

            for(int y=top; y<bottom; ++y)
            {
                for (int x = left; x < right; ++x)
                {
                    glyph = m[x + y * w].GetGlyph(out lit);
                    Console.buf[x - offset.x - buf_offset.x + (y - offset.y - buf_offset.y) * Console.Width].Set(glyph, (short)(lit ? ConsoleColor.Gray : ConsoleColor.DarkBlue));
                }
            }
        }

        public bool CanMove(Pos old_pos, Pos new_pos, bool diagonal)
        {
            Tile t = m[new_pos.x + new_pos.y * w];
            if(t.unit == null && t.CanMove())
            {
                if (!diagonal)
                    return true;
                if (m[old_pos.x + new_pos.y * w].CanMove() || m[new_pos.x + old_pos.y * w].CanMove())
                    return true;
            }
            return false;
        }

        public Tile GetTileSafe(Pos pos)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= w || pos.y >= h)
                return null;
            else
                return m[pos.x + pos.y * w];
        }

        public IEnumerable<Tile> GetNearTiles(Pos pos)
        {
            if(pos.x > 0)
            {
                if (pos.y > 0)
                    yield return m[pos.x - 1 + (pos.y - 1) * w];
                yield return m[pos.x - 1 + pos.y * w];
                if (pos.y < h)
                    yield return m[pos.x - 1 + (pos.y + 1) * w];
            }
            if(pos.x < w)
            {
                if (pos.y > 0)
                    yield return m[pos.x + 1 + (pos.y - 1) * w];
                yield return m[pos.x + 1 + pos.y * w];
                if (pos.y < h)
                    yield return m[pos.x + 1 + (pos.y + 1) * w];
            }
            if (pos.y > 0)
                yield return m[pos.x + (pos.y - 1) * w];
            if (pos.y < h)
                yield return m[pos.x + (pos.y + 1) * w];
        }

        public void Save(BinaryWriter f)
        {
            f.Write(w);
            f.Write(h);
            for (int i = 0; i < w * h; ++i)
                m[i].Save(f);
        }

        public void Load(BinaryReader f)
        {
            f.Read(out w);
            f.Read(out h);
            if (w < 5 || h < 5 || w > 200 || h > 200)
                throw new Exception(string.Format("Invalid map size {0}, {1}.", w, h));
            m = new List<Tile>();
            for (int i = 0; i < w * h; ++i)
            {
                Tile t = new Tile();
                t.Load(f);
                m.Add(t);
            }
        }
        
        void CreateMask(int radius)
        {
            int size = radius*2+1;
            mask = new bool[size, size];
            for(int y=0; y<size; ++y)
            {
                for (int x = 0; x < size; ++x)
                    mask[x, y] = false;
            }

            foreach (var a in Utils.DrawCircle(new Pos(radius, radius), radius))
                mask[a.x, a.y] = true;

            int left, right;

            for(int y=0; y<size; ++y)
            {
                left = -1;
                right = -1;
                for(int x=0; x<size; ++x)
                {
                    if(mask[x,y])
                    {
                        left = x;
                        break;
                    }
                }
                for(int x=size-1; x>0; --x)
                {
                    if(mask[x,y])
                    {
                        right = x;
                        break;
                    }
                }
                if(left != -1 && right != -1 && right > left)
                {
                    for(int x=left; x<=right; ++x)
                        mask[x,y] = true;
                }
            }

            mask_radius = radius;
        }

	    struct Line
	    {
            public Pos near, far;

		    public bool isBelow(Pos pt)
		    {
			    return relativeSlope(pt) > 0;
		    }

		    public bool isBelowOrContains(Pos pt)
		    {
			    return relativeSlope(pt) >= 0;
		    }

		    public bool isAbove(Pos pt)
		    {
			    return relativeSlope(pt) < 0;
		    }

		    public bool isAboveOrContains(Pos pt)
		    {
			    return relativeSlope(pt) <= 0;
		    }

		    public bool doesContain(Pos pt)
		    {
			    return relativeSlope(pt) == 0;
		    }

		    // negative if the line is above the point.
		    // positive if the line is below the point.
		    // 0 if the line is on the point.
		    public int relativeSlope(Pos pt)
		    {
			    return (far.y - near.y)*(far.x - pt.x)
				    - (far.y - pt.y)*(far.x - near.x);
		    }		    
	    };

	    class Bump
	    {
		    public Pos pos;
		    public Bump parent;

            public Bump(Pos _pos, Bump _parent)
            {
                pos = _pos;
                parent = _parent;
            }
	    };

	    class Field
	    {
		    public Line steep, shallow;
		    public Bump steepBump, shallowBump;

            public Field Copy()
            {
                return new Field
                {
                    steep = steep,
                    shallow = shallow,
                    steepBump = steepBump,
                    shallowBump = shallowBump
                };
            }
	    };

        Pos source, extent, quadrant;
	    List<Bump> steepBumps = new List<Bump>();
	    List<Bump> shallowBumps = new List<Bump>();
	    LList<Field> activeFields = new LList<Field>();
        bool[,] mask;
        int mask_radius = -1;
        Pos shallowOrigin, steepOrigin;

        const int fov_type = 4; // [0-8]
        const int fov_size = 16;
        const int fov_sourceRange = fov_type * 2;
        const int fov_destRange = fov_type * 2;
        const int fov_sourceOffset = (fov_size - fov_sourceRange) / 2;
        const int fov_sourceLimit = fov_size - fov_sourceOffset;
        const int fov_destOffset = (fov_size - fov_destRange) / 2;
        const int fov_destLimit = fov_size - fov_destOffset;

	    bool isBlocked(int x, int y)
	    {
		    if(x < 0 || y < 0 || x >= w || y >= h)
			    return true;

            Tile t = m[x+y*w];

            if(t.type == Tile.Type.Door)
            {
                if ((t.flags & Tile.Flags.Open) != 0)
                    return false;
            }

            return t.type != Tile.Type.Empty;
	    }

        bool doesPermissiveVisit(int x, int y)
        {
            return !mask[x + mask_radius, y + mask_radius];
        }

        void visit(Pos pos)
        {
            Tile t = m[pos.x + pos.y * w];
            t.flags |= Tile.Flags.Known | Tile.Flags.Lit;
        }

        void checkVisit(Pos pos, Pos adjustedPos)
        {
            if (adjustedPos.x < 0 || adjustedPos.y < 0 || adjustedPos.x >= w || adjustedPos.y >= w)
                return;

            if (!((quadrant.x * quadrant.y == 1 && pos.x == 0 && pos.y != 0)
              || (quadrant.x * quadrant.y == -1 && pos.y == 0 && pos.x != 0)
              || doesPermissiveVisit(pos.x/fov_size * quadrant.x, pos.y/fov_size * quadrant.y)))
                visit(adjustedPos);
        }

	    bool actIsBlocked(Pos pos, Field currentField)
	    {
            Pos adjustedPos = new Pos(pos.x/fov_size*quadrant.x + source.x, pos.y/fov_size*quadrant.y + source.y);
            bool result = isBlocked(adjustedPos.x, adjustedPos.y);
            Pos topLeft;
            Pos bottomRight;
            if (result)
            {
                topLeft = new Pos(pos.x, pos.y + fov_size);
                bottomRight = new Pos(pos.x + fov_size, pos.y);
            }
            else
            {
                topLeft = new Pos(pos.x + fov_destOffset, pos.y + fov_destLimit);
                bottomRight = new Pos(pos.x + fov_destLimit, pos.y + fov_destOffset);
            }
            if (currentField.steep.isAbove(bottomRight) && currentField.shallow.isBelow(topLeft))
                checkVisit(pos, adjustedPos);
            return result;
	    }

        LList<Field>.Iterator checkField(LList<Field>.Iterator currentField)
	    {
		    // If the two slopes are colinear, and if they pass through either
		    // extremity, remove the field of view.
            if (currentField.Current.shallow.doesContain(currentField.Current.steep.near)
                && currentField.Current.shallow.doesContain(currentField.Current.steep.far)
                && (currentField.Current.shallow.doesContain(shallowOrigin)
                || currentField.Current.shallow.doesContain(steepOrigin)))
            {
                return activeFields.Erase(currentField);
            }
            else
                return currentField;
	    }

	    void addShallowBump(Pos pos, Field currentField)
	    {
		    // First, the far point of shallow is set to the new point.
		    currentField.shallow.far = pos;
		    // Second, we need to add the new bump to the shallow bump list for
		    // future steep bump handling.
            Bump bump = new Bump(pos, currentField.shallowBump);
            shallowBumps.Add(bump);
            currentField.shallowBump = bump;
		    // Now we have too look through the list of steep bumps and see if
		    // any of them are below the line.
		    // If there are, we need to replace near point too.
		    Bump currentBump = currentField.steepBump;
		    while(currentBump != null)
		    {
			    if(currentField.shallow.isAbove(currentBump.pos))
				    currentField.shallow.near = currentBump.pos;
			    currentBump = currentBump.parent;
		    }
	    }

	    void addSteepBump(Pos pos, Field currentField)
	    {
		    currentField.steep.far = pos;
            Bump bump = new Bump(pos, currentField.steepBump);
            steepBumps.Add(bump);
		    currentField.steepBump = bump;
		    // Now look through the list of shallow bumps and see if any of them
		    // are below the line.
		    Bump currentBump = currentField.shallowBump;
		    while (currentBump != null)
		    {
			    if(currentField.steep.isBelow(currentBump.pos))
				    currentField.steep.near = currentBump.pos;
			    currentBump = currentBump.parent;
		    }
	    }

	    void visitSquare(Pos dest, ref LList<Field>.Iterator currentField)
	    {
            //Debug.Print(string.Format("{0}, {1}", dest.x, dest.y));

		    // The top-left and bottom-right corners of the destination square.
		    Pos topLeft = new Pos(dest.x, dest.y + fov_size);
		    Pos bottomRight = new Pos(dest.x + fov_size, dest.y);
            LList<Field>.Iterator end = activeFields.End();
		    while (currentField != end && currentField.Current.steep.isBelowOrContains(bottomRight))
		    {
                //Debug.Print("ABOVE");
			    // case ABOVE
			    // The square is in case 'above'. This means that it is ignored
			    // for the currentField. But the steeper fields might need it.
			    ++currentField;
		    }

		    if (currentField == activeFields.End())
		    {
                //Debug.Print("ABOVE ALL");
			    // The square was in case 'above' for all fields. This means that
			    // we no longer care about it or any squares in its diagonal rank.
			    return;
		    }

		    // Now we check for other cases.
		    if (currentField.Current.shallow.isAboveOrContains(topLeft))
		    {
                //Debug.Print("BELOW");
			    // case BELOW
			    // The shallow line is above the extremity of the square, so that
			    // square is ignored.
			    return;
		    }
		    // The square is between the lines in some way. This means that we
		    // need to visit it and determine whether it is blocked.
            bool isBlocked = actIsBlocked(dest, currentField.Current);
            if (!isBlocked)
		    {
                //Debug.Print("NOT BLOCKED");
			    // We don't care what case might be left, because this square does
			    // not obstruct.
			    return;
		    }

		    if (currentField.Current.shallow.isAbove(bottomRight) && currentField.Current.steep.isBelow(topLeft))
		    {
                //Debug.Print("BLOCKING");
			    // case BLOCKING
			    // Both lines intersect the square. This current field has ended.
                currentField = activeFields.Erase(currentField);
		    }
		    else if (currentField.Current.shallow.isAbove(bottomRight))
		    {
                //Debug.Print("SHALLOW BUMP");
			    // case SHALLOW BUMP
			    // The square intersects only the shallow line.
			    addShallowBump(topLeft, currentField.Current);
			    currentField = checkField(currentField);
		    }
		    else if (currentField.Current.steep.isBelow(topLeft))
		    {
                //Debug.Print("STEEP BUMP");
			    // case STEEP BUMP
			    // The square intersects only the steep line.
			    addSteepBump(bottomRight, currentField.Current);
			    checkField(currentField);
		    }
		    else
		    {
               // Debug.Print("BETWEEN");
			    // case BETWEEN
			    // The square intersects neither line. We need to split into two fields.
			    LList<Field>.Iterator steeperField = currentField;
                LList<Field>.Iterator shallowerField = activeFields.Insert(currentField, currentField.Current.Copy());
                addSteepBump(bottomRight, shallowerField.Current);
                checkField(shallowerField);
                addShallowBump(topLeft, steeperField.Current);
                currentField = checkField(steeperField);
		    }
	    }

	    void calculateFovQuadrant()
	    {
            Field field = new Field();
            activeFields.Add(field);
            field.shallow.near = shallowOrigin;
		    field.shallow.far = new Pos(fov_size, 0);
		    field.steep.near = steepOrigin;
		    field.steep.far = new Pos(0, fov_size);
            
		    // Visit the source square exactly once (in quadrant 1).
            if (quadrant.x == 1 && quadrant.y == 1)
                visit(source);

            var currentField = activeFields.Begin();
            int i, j, maxI = extent.x + extent.y;
            Pos dest = new Pos();

		    // For each square outline
		    for (i = 1; i <= maxI && activeFields.Count > 0; ++i)
		    {
			    int startJ = Math.Max(0, i - extent.x);
			    int maxJ = Math.Min(i, extent.y);
			    // Visit the nodes in the outline
			    for (j = startJ; j <= maxJ && currentField != activeFields.End(); ++j)
			    {
				    dest.x = (i-j) * fov_size;
                    dest.y = j * fov_size;
				    visitSquare(dest, ref currentField);
			    }
			    currentField = activeFields.Begin();
		    }

            steepBumps.Clear();
		    shallowBumps.Clear();
		    activeFields.Clear();
	    }

	    public void CalculateFov(Pos pos, int radius)
	    {
            if (radius != mask_radius)
                CreateMask(radius);

            for (int i = 0; i < w * h; ++i)
                m[i].flags &= ~Tile.Flags.Lit;

            source = pos;
            shallowOrigin = new Pos(fov_sourceOffset, fov_sourceLimit);
            steepOrigin = new Pos(fov_sourceLimit, fov_sourceOffset);
            extent = new Pos(radius, radius);

		    quadrant = new Pos(1,1);
			calculateFovQuadrant();

            quadrant = new Pos(-1,1);
			calculateFovQuadrant();

            quadrant = new Pos(-1,-1);
			calculateFovQuadrant();

            quadrant = new Pos(1,-1);
			calculateFovQuadrant();
	    }
    }
}
