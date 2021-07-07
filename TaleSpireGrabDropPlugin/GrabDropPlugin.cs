using UnityEngine;
using BepInEx;
using Bounce.Unmanaged;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Configuration;

namespace LordAshes
{
    [BepInPlugin(Guid, "Grab Drop Plug-In", Version)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    [BepInDependency(StatMessaging.Guid)]
    [BepInDependency(FileAccessPlugin.Guid)]
    public class GrabDropPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Guid = "org.lordashes.plugins.grabdrop";
        public const string Version = "1.0.0.0";

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Track radial asset
        private CreatureGuid radialCreature = CreatureGuid.Empty;

        private ConfigEntry<bool> actOnMini;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes Grab Drop Plugin Active.");

            // Post plugin on TS main page
            StateDetection.Initialize(this.GetType());

            // Get operation order
            actOnMini = Config.Bind("Settings", "Select prop, action on mini", true);

            // Create grab menu entry
            RadialUI.RadialUIPlugin.AddOnCharacter(Guid + ".grab", new MapMenu.ItemArgs
            {
                Action = (mmi, obj) =>
                {
                    if (actOnMini.Value)
                    {
                        StatMessaging.SetInfo(radialCreature, GrabDropPlugin.Guid, "Grab," + LocalClient.SelectedCreatureId.ToString());
                    }
                    else
                    {
                        StatMessaging.SetInfo(LocalClient.SelectedCreatureId, GrabDropPlugin.Guid, "Grab," + radialCreature.ToString());
                    }
                },
                Icon = FileAccessPlugin.Image.LoadSprite("Grab.png"),
                Title = "Grab",
                CloseMenuOnActivate = true
            }, Reporter);

            // Create drop menu entry
            RadialUI.RadialUIPlugin.AddOnCharacter(Guid + ".drop", new MapMenu.ItemArgs
            {
                Action = (mmi, obj) =>
                {
                    if (actOnMini.Value)
                    {
                        StatMessaging.SetInfo(radialCreature, GrabDropPlugin.Guid, "Drop,");
                    }
                    else
                    {
                        StatMessaging.SetInfo(LocalClient.SelectedCreatureId, GrabDropPlugin.Guid, "Drop,"+radialCreature.ToString());
                    }
                },
                Icon = FileAccessPlugin.Image.LoadSprite("Drop.png"),
                Title = "Drop",
                CloseMenuOnActivate = true
            }, Reporter);

            // Subscribe to grab and drop requests
            StatMessaging.Subscribe(GrabDropPlugin.Guid, (changes) =>
            {
                foreach (StatMessaging.Change change in changes)
                {
                    try
                    {
                        string[] values = change.value.Split(',');

                        if (values[0] == "Grab")
                        {
                            // Grab
                            CreatureBoardAsset asset;
                            CreaturePresenter.TryGetAsset(change.cid, out asset);
                            if (asset != null)
                            {
                                CreatureBoardAsset child;
                                CreaturePresenter.TryGetAsset(new CreatureGuid(values[1]), out child);
                                if (child != null)
                                {
                                    Debug.Log(StatMessaging.GetCreatureName(asset.Creature) + " grabs '" + child.Creature.Name + "'");
                                    child.transform.SetParent(asset.transform);
                                }
                            }
                        }
                        else if (values[0] == "Drop")
                        {
                            // Drop
                            CreatureBoardAsset asset;
                            CreaturePresenter.TryGetAsset(change.cid, out asset);
                            foreach (Transform child in asset.transform.Children())
                            {
                                if ((child.gameObject.name.StartsWith("Custom:")) || (child.gameObject.name.Contains("(Clone)")))
                                {
                                    Debug.Log(StatMessaging.GetCreatureName(asset.Creature) + " drops '" + child.gameObject.name + "'");
                                    child.transform.SetParent(null);
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("Error: Unknown value prefix in GrabDrop while resolving action: " + values[0]);
                        }
                    }catch(Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            });
        }

        /// <summary>
        /// Method to track which asset has the radial menu open
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="radialMenu"></param>
        /// <returns></returns>
        private bool Reporter(NGuid selected, NGuid radialMenu)
        {
            radialCreature = new CreatureGuid(radialMenu);
            return true;
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
        }
    }
}
