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

using UnityEngine;

namespace Kopernicus
{
	// Mod runtime utilitues
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class RuntimeUtility : MonoBehaviour
	{
		/**
		 * Cache of the line renderers for PQS quads debug
		 **/
		private List<GameObject> quadSurfaceNormalRenderers = new List<GameObject>();
		
		/**
		 * Shared instance of the surface normal renderer material.  Use the property accessor
		 **/
		private Material sharedSurfaceNormalRendererMaterial = null;
		
		/**
		 * Purge the surface normal renderer list
		 **/
		private void PurgeQuadSurfaceNormalRenderers()
		{
			foreach (GameObject r in quadSurfaceNormalRenderers) 
			{
				r.transform.parent = null;
				Destroy(r);
			}
			quadSurfaceNormalRenderers.Clear ();
		}
		
		/**
		 * Get the material used for the line renderer
		 **/
		private Material surfaceNormalRendererMaterial
		{
			get
			{
				if(sharedSurfaceNormalRendererMaterial == null)
				{
					sharedSurfaceNormalRendererMaterial = new Material( Shader.Find( "Particles/Additive" ) );
				}
				return sharedSurfaceNormalRendererMaterial;
			}
		}
		
		/**
		 * Get a surface normal renderer (either from cache, or create a new one)
		 **/
		private GameObject surfaceNormalRenderer
		{
			get
			{
				// The renderer to return
				GameObject r = null;
				
				// If there are renderers in the cache, use it
				if(quadSurfaceNormalRenderers.Count > 0)
				{
					r = quadSurfaceNormalRenderers[0];
					r.SetActive(true);
					quadSurfaceNormalRenderers.RemoveAt(0);
					return r;
				}
				
				// Otherwise allocate a new one
				else
				{
					// Create a new game object to hold this renderer
					r = new GameObject("__Debug");
					DontDestroyOnLoad(r);
					LineRenderer line = r.AddComponent<LineRenderer>();
					
					// Make it render a red to yellow triangle, 1 meter wide and 2 meters long
					line.sharedMaterial = surfaceNormalRendererMaterial;
					line.useWorldSpace = false;
					line.SetColors( Color.green, Color.green );
					line.SetWidth(50, 50);
					line.SetVertexCount( 2 );
					line.SetPosition( 0, Vector3.zero);
					line.SetPosition( 1, Vector3.forward * -500.0f );
				}
				
				// Return the transform
				return r;
			}
		}
		
		/**
		 * If Mod-P are pressed, dump the PQS data of the current live body
		 **/
		public void Update()
		{
			bool isModDown = GameSettings.MODIFIER_KEY.GetKey();
            
			// Print out the PQS state
			if (Input.GetKeyDown(KeyCode.P) && isModDown)
			{
				// Log the state of the PQS
				Utility.DumpObjectFields(FlightGlobals.currentMainBody.pqsController, " Live PQS ");

				// Dump the child PQSs
				foreach(PQS p in FlightGlobals.currentMainBody.pqsController.ChildSpheres)
				{
					// Dump the child PQS info
					Utility.DumpObjectFields(p, " " + p.ToString() + " ");
					
					// Dump components of this child
					foreach(PQSMod m in p.GetComponentsInChildren<PQSMod>())
					{
						Utility.DumpObjectFields(m, " " + m.ToString() + " ");
					}
					
					// Print out information on all of the quads in the child
					foreach(PQ q in p.GetComponentsInChildren<PQ>())
					{
						// Log information about the quad
						Debug.Log("Quad \"" + q.name + "\" = (" + q.meshRenderer.enabled + ",forced=" + q.isForcedInvisible + ";" + q.meshRenderer.material.name + ") @ " + q.transform.position);
					}
				}
			}

			// If we want to debug the locations of PQ nodes (Mod-;)
			if (Input.GetKeyDown(KeyCode.Semicolon) && isModDown)
			{
				// New list for the debugger 
				List<GameObject> renderers = new List<GameObject>();

				// Draw renderers for all PQ nodes in play on this body
				foreach(PQ q in FlightGlobals.currentMainBody.pqsController.GetComponentsInChildren<PQ>())
				{
					// Add the line renderer to this quad
					GameObject r = surfaceNormalRenderer;
					r.transform.parent = q.transform;
					
					// Initialize the local transform to a zero transform
					r.transform.localPosition    = Vector3.zero;
					r.transform.localScale       = Vector3.one;
					r.transform.localEulerAngles = Vector3.zero; 
					
					// Add to the new renderers list
					renderers.Add(r);
				}
				
				// Purge the old renderers list
				PurgeQuadSurfaceNormalRenderers();
				quadSurfaceNormalRenderers = renderers;

				// Log
				Debug.Log("[Kopernicus]: RuntimeUtility.Update(): " + renderers.Count + " PQ surface normal renderer(s) created");
			}

			// If we want to clean up (Mod-/)
			if (Input.GetKeyDown(KeyCode.Slash) && isModDown)
			{
				// Disable all of the renderers in the list
				foreach(GameObject r in quadSurfaceNormalRenderers)
				{
					r.transform.parent = null;
					r.SetActive(false);
				}
				
				// Log
				Debug.Log("[Kopernicus]: RuntimeUtility.Update(): " + quadSurfaceNormalRenderers.Count + " PQ surface normal renderer(s) disabled");
			}
		}
		
		/**
		 * Awake() - flag this class as don't destroy on load
		 **/
		public void Awake ()
		{
			// Make sure the runtime utility isn't killed
			DontDestroyOnLoad (this);
			
			// Log
			Debug.Log ("[Kopernicus]: RuntimeUtility Started");

			// Log the reference ids
			foreach (CelestialBody body in PSystemManager.Instance.localBodies) 
			{
				Debug.Log("Body \"" + body.name + "\" = " + PSystemManager.Instance.localBodies.IndexOf(body) + ", SOI = " + body.sphereOfInfluence);
			}
		}
	}
}

