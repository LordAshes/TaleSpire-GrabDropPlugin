using UnityEngine;
using BepInEx;
using Bounce.Unmanaged;
using System.Collections.Generic;
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
                        StatMessaging.SetInfo(radialCreature, GrabDropPlugin.Guid + ".grab", LocalClient.SelectedCreatureId.ToString());
                    }
                    else
                    {
                        StatMessaging.SetInfo(LocalClient.SelectedCreatureId, GrabDropPlugin.Guid + ".grab", radialCreature.ToString());
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
                        StatMessaging.SetInfo(radialCreature, GrabDropPlugin.Guid + ".drop", LocalClient.SelectedCreatureId.ToString());
                    }
                    else
                    {
                        StatMessaging.SetInfo(LocalClient.SelectedCreatureId, GrabDropPlugin.Guid + ".drop", radialCreature.ToString());
                    }
                },
                Icon = FileAccessPlugin.Image.LoadSprite("Drop.png"),
                Title = "Drop",
                CloseMenuOnActivate = true
            }, Reporter);

            // Create safe Kill menu entry      
            RadialUI.RadialUIPlugin.AddOnRemoveSubmenuKill(Guid + ".removeKill", "Kill Creature"); //Remove vanilla TS Kill Menu
            RadialUI.RadialUIPlugin.AddOnSubmenuKill(Guid + ".safeKill", new MapMenu.ItemArgs //Replace with GrabDrop safe alternative
            {
                Action = (mmi, obj) =>
                {
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(radialCreature, out asset);

                    if (asset != null)
                    {
                        try
                        {
                            Debug.Log("Beginning GrabDrop Safe Kill");
                            List<Transform> tempTransformList = new List<Transform>();

                            //Find certain transforms to preserve
                            foreach (Transform child in asset.gameObject.transform.Children())
                            {
                                //Debug.Log("Transform Name: " + child.name + " , Game Object Name: " + child.gameObject.name);
                                if (child.name == "MoveableOffset")
                                {
                                    //Debug.Log("Preserving '" + child.name + "' transform for safe delete");
                                    tempTransformList.Add(child);
                                }
                            }

                            //It's imperative we remove the parent child relationship with any dragged creatures before deleting the parent
                            asset.gameObject.transform.DetachChildren();  

                            //Certain transforms must be preserved to be gracefully disposed of by the engine when the parent object is deleted
                            if (tempTransformList.Count > 0)
                            {   
                                foreach (Transform tempTransform in tempTransformList)
                                {
                                    tempTransform.SetParent(asset.gameObject.transform);
                                }
                            }

                            asset.Creature.BoardAsset.RequestDelete(); //Perform the actual deletion
                            Debug.Log("GrabDrop Safe Kill Complete");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                },
                Icon = Icons.GetIconSprite("remove"),
                Title = "Kill Creature",
                CloseMenuOnActivate = true
            }, Reporter);

            // Subscribe to grab requests
            StatMessaging.Subscribe(GrabDropPlugin.Guid + ".grab", (changes) =>
            {
                foreach (StatMessaging.Change change in changes)
                {
                    if (change.action == StatMessaging.ChangeType.removed)
                    {
                        //Change skipped in Grab Callback due to it being a remove which we do not currently care about
                        continue;
                    }
                    try
                    {
                        // Grab
                        CreatureBoardAsset asset;
                        CreaturePresenter.TryGetAsset(change.cid, out asset);
                        if (asset != null)
                        {
                            CreatureBoardAsset child;
                            CreaturePresenter.TryGetAsset(new CreatureGuid(change.value), out child);
                            if (child != null)
                            {
                                Debug.Log(StatMessaging.GetCreatureName(asset.Creature) + " grabs '" + child.Creature.Name + "'");
                                child.transform.SetParent(asset.transform);
                                StatMessaging.ClearInfo(change.cid, GrabDropPlugin.Guid + ".drop");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            });

            // Subscribe to drop requests
            StatMessaging.Subscribe(GrabDropPlugin.Guid + ".drop", (changes) =>
            {
                foreach (StatMessaging.Change change in changes)
                {
                    if (change.action == StatMessaging.ChangeType.removed)
                    {
                        //Change skipped in Drop Callback due to it being a remove which we do not currently care about
                        continue;
                    }
                    try
                    {
                        // Drop
                        CreatureBoardAsset asset;
                        CreaturePresenter.TryGetAsset(change.cid, out asset);
                        if (asset != null)
                        {
                            CreatureBoardAsset droppedChild;
                            CreaturePresenter.TryGetAsset(new CreatureGuid(change.value), out droppedChild);
                            foreach (Transform child in asset.transform.Children())
                            {
                                if (child.gameObject == droppedChild.gameObject)
                                {
                                    Debug.Log(StatMessaging.GetCreatureName(asset.Creature) + " drops '" + StatMessaging.GetCreatureName(droppedChild.Creature) + "'");
                                    child.transform.SetParent(null);
                                    StatMessaging.ClearInfo(change.cid, GrabDropPlugin.Guid + ".grab");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
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
