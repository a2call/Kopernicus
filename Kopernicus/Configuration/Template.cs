/** 
 * Kopernicus Planetary System Modifier
 * Copyright (C) 2014 Bryce C Schroeder (bryce.schroeder@gmail.com), Nathaniel R. Lewis (linux.robotdude@gmail.com)
 * 
 * http://www.ferazelhosting.net/~bryce/contact.html
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2014 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Kopernicus
{
	namespace Configuration
	{
		[RequireConfigType(ConfigType.Node)]
		public class Template : IParserEventSubscriber
		{
			// Cloned PSystemBody to expose to the config system
			public PSystemBody body;

			// Initial radius of the body
			public double radius { get; private set; }

			// Initial type of the body
			public BodyType type { get; private set; }

			// PSystemBody to use as a template in lookup & clone
			private PSystemBody originalBody; 

			// Name of the body to use for the template
			[PreApply]
			[ParserTarget("name", optional = false)]
			private string name 
			{
				// Crawl the system prefab for the body
				set 
				{ 
					originalBody = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, value);
					if(originalBody == null)
					{
						throw new TemplateNotFoundException("Unable to find: " + value);
					}
				}
			}

			// Should we strip the atmosphere off
			[ParserTarget("removeAtmosphere", optional = true)]
			private NumericParser<bool> removeAtmosphere = new NumericParser<bool>(false);

			// Should we strip the ocean off
			[ParserTarget("removeOcean", optional = true)]
			private NumericParser<bool> removeOcean = new NumericParser<bool>(false);


			// Collection of PQS mods to remove 
			// something about a collection (probably strings)
			// a collection of mods

			// Apply event
			void IParserEventSubscriber.Apply (ConfigNode node)
			{
				// Instantiate (clone) the template body
				GameObject bodyGameObject = UnityEngine.Object.Instantiate (originalBody.gameObject) as GameObject;
				bodyGameObject.name = originalBody.name;
				bodyGameObject.transform.parent = Utility.Deactivator;
				body = bodyGameObject.GetComponent<PSystemBody> ();
				body.children = new List<PSystemBody> ();

				// Clone the scaled version
				body.scaledVersion = UnityEngine.Object.Instantiate (originalBody.scaledVersion) as GameObject;
				body.scaledVersion.transform.parent = Utility.Deactivator;
				body.scaledVersion.name = originalBody.scaledVersion.name;
				
				// Clone the PQS version (if it has one)
				if (body.pqsVersion != null) 
				{
					body.pqsVersion = UnityEngine.Object.Instantiate (originalBody.pqsVersion) as PQS;
					body.pqsVersion.transform.parent = Utility.Deactivator;
					body.pqsVersion.name = originalBody.pqsVersion.name;
				}

				// Store the initial radius (so scaled version can be computed)
				radius = body.celestialBody.Radius;
			}

			// Post apply event
			void IParserEventSubscriber.PostApply (ConfigNode node)
			{
				// Should we remove the atmosphere
				if (body.celestialBody.atmosphere && removeAtmosphere.value) 
				{
					// Find atmosphere from ground and destroy the game object
					AtmosphereFromGround atmosphere = body.scaledVersion.GetComponentsInChildren<AtmosphereFromGround>(true)[0];
					atmosphere.transform.parent = null;
					UnityEngine.Object.Destroy(atmosphere.gameObject);

					// Destroy the light controller
					MaterialSetDirection light = body.scaledVersion.GetComponentsInChildren<MaterialSetDirection>(true)[0];
					UnityEngine.Object.Destroy(light);

					// No more atmosphere :(
					body.celestialBody.atmosphere = false;
				}

				// Should we remove the ocean?
				if (body.celestialBody.ocean && removeOcean.value) 
				{
					// Find atmosphere the ocean PQS
					PQS ocean = body.pqsVersion.GetComponentsInChildren<PQS>(true).Where(pqs => pqs != body.pqsVersion).First();
					PQSMod_CelestialBodyTransform cbt = body.pqsVersion.GetComponentsInChildren<PQSMod_CelestialBodyTransform>(true).First();

					// Destroy the ocean PQS (this could be bad - destroying the secondary fades...)
					cbt.planetFade.secondaryRenderers.Remove(ocean.gameObject);
					cbt.secondaryFades = null;
					ocean.transform.parent = null;
					UnityEngine.Object.Destroy(ocean);
					
					// No more ocean :(
					body.celestialBody.ocean = false;
				}
				
				// Figure out what kind of body we are
				if (body.scaledVersion.GetComponentsInChildren(typeof(ScaledSun), true).Length > 0)
					type = BodyType.Star;
				else if(body.celestialBody.atmosphere)
					type = BodyType.Atmospheric;
				else
					type = BodyType.Vacuum;

				Debug.Log ("[Kopernicus]: Configuration.Template: Using Template \"" + body.celestialBody.bodyName + "\"");
			}

			// Private exception to throw in the case the template doesn't load
			private class TemplateNotFoundException : Exception
			{
				public TemplateNotFoundException(string s) : base(s)
				{

				}
			}
		}
	}
}

