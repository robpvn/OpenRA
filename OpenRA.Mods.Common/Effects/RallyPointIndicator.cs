#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	class RallyPointIndicator : IEffect, IEffectAboveShroud, IEffectAnnotation
	{
		readonly Actor building;
		readonly RallyPoint rp;
		readonly Animation flag;
		readonly Animation circles;
		readonly ExitInfo[] exits;

		readonly WPos[] targetLine = new WPos[2];
		CPos cachedLocation;

		public RallyPointIndicator(Actor building, RallyPoint rp, ExitInfo[] exits)
		{
			this.building = building;
			this.rp = rp;
			this.exits = exits;

			if (rp.Info.Image != null)
			{
				flag = new Animation(building.World, rp.Info.Image);
				flag.PlayRepeating(rp.Info.FlagSequence);

				circles = new Animation(building.World, rp.Info.Image);
				circles.Play(rp.Info.CirclesSequence);
			}
		}

		void IEffect.Tick(World world)
		{
			if (flag != null)
				flag.Tick();

			if (circles != null)
				circles.Tick();

			if (cachedLocation != rp.Location)
			{
				cachedLocation = rp.Location;

				var rallyPos = world.Map.CenterOfCell(cachedLocation);
				var exitPos = building.CenterPosition;

				// Find closest exit
				var dist = int.MaxValue;
				foreach (var exit in exits)
				{
					var ep = building.CenterPosition + exit.SpawnOffset;
					var len = (rallyPos - ep).Length;
					if (len < dist)
					{
						dist = len;
						exitPos = ep;
					}
				}

				targetLine[0] = exitPos;
				targetLine[1] = rallyPos;

				if (circles != null)
					circles.Play(rp.Info.CirclesSequence);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			var renderables = SpriteRenderable.None;
			if (circles != null || flag != null)
			{
				var palette = wr.Palette(rp.PaletteName);
				if (circles != null)
					renderables = renderables.Concat(circles.Render(targetLine[1], palette));

				if (flag != null)
					renderables = renderables.Concat(flag.Render(targetLine[1], palette));
			}

			return renderables;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (Game.Settings.Game.TargetLines == TargetLinesType.Disabled)
				return SpriteRenderable.None;

			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			return new IRenderable[] { new TargetLineRenderable(targetLine, building.Owner.Color, rp.Info.LineWidth) };
		}
	}
}
