using System;
using System.Windows.Forms;
using Rage;
using Rage.Attributes;
using Rage.Native;

[assembly: Plugin("Debugg Crouch", Description = "Allows for crouching and weapon aiming | Made by Debugg#8770.", Author = "Debugg")]
namespace Debugg_crouch
{
    public class EntryPoint
    {
        public static bool crouch = false;
        public static bool proned = false;
        public static dynamic N = NativeFunction.Natives;
        public static Keys crouchKey = Keys.X;
        public static Keys proneKey = Keys.Y;
        public static bool movefwd;
        public static bool movebwd;

        public static InitializationFile initialiseFile()
        {
            InitializationFile ini = new InitializationFile("Plugins/DebuggCrouch.ini");
            ini.Create();
            return ini;
        }

        public static void RegisterKeyMapping()
        {
            InitializationFile ini = initialiseFile();
            KeysConverter kc = new KeysConverter();
            crouchKey = (Keys)kc.ConvertFromString(ini.ReadString("Keybindings", "crouchKey", "X"));
            proneKey = (Keys)kc.ConvertFromString(ini.ReadString("Keybindings", "proneKey", "Y"));
        }

        public static void crouchC()
        {
            Ped myChar = Game.LocalPlayer.Character;
            N.ResetPedMovementClipset(myChar, 0f);
            N.ResetPedWeaponMovementClipset(myChar);
            N.ResetPedStrafeClipset(myChar);
        }

        public static void proneC()
        {
            Ped myChar = Game.LocalPlayer.Character;
            myChar.Tasks.Clear();
            Vector3 me = N.GetEntityCoords<Vector3>(myChar);
            N.SetEntityCoords(myChar, me.X, me.Y, me.Z -0.5f);
        }

        public static void Main()
        {
            RegisterKeyMapping();
            GameFiber.StartNew(ProcessCrouch);
            GameFiber.StartNew(ProcessProne);
        }

        public static void ProneMovement()
        {
            Ped myChar = Game.LocalPlayer.Character;
            float heading = N.GetEntityHeading<float>(myChar);
            if (Game.IsControlJustPressed(0, (GameControl)32) || Game.IsControlJustPressed(0, (GameControl)33))
            {
                N.DisablePlayerFiring(myChar, true);
            } 
            
            if (Game.IsControlJustReleased(0, (GameControl) 32) || Game.IsControlJustReleased(0, (GameControl)33))
            {
                N.DisablePlayerFiring(myChar, false);
                myChar.Tasks.Clear();
                if (N.IsPedArmed<bool>(myChar, 4 | 2)) {
                    N.TaskAimGunScripted(myChar, N.GetHashKey<uint>("SCRIPTED_GUN_TASK_PLANE_WING"), true, true);
                } else
                {
                    N.TaskPlayAnim(myChar, "move_crawl", "onfront_fwd", 8f, -8f, -1, 2, 0.0f, false, false, false);
                }
            }

            if (Game.IsControlJustPressed(0, (GameControl)32))
            {
                movefwd = true;
                N.TaskPlayAnim(myChar, "move_crawl", "onfront_fwd", 8f, -4f, -1, 9, 0.0f, false, false, false);
            }

            if (Game.IsControlJustPressed(0, (GameControl)33))
            {
                N.TaskPlayAnim(myChar, "move_crawl", "onfront_bwd", 8f, -4f, -1, 9, 0.0f, false, false, false);
            }

            if (Game.GetKeyboardState().IsDown(Keys.A))
            {
                N.SetEntityHeading(myChar, heading + 2.0f);
            }
            else if (Game.GetKeyboardState().IsDown(Keys.D))
            {
                N.SetEntityHeading(myChar, heading - 2.0f);
            }
        }

        public static void ProcessCrouch()
        {
            while (true)
            {
                GameFiber.Yield();
                GameFiber.Wait(0);
                Ped myChar = Game.LocalPlayer.Character;

                if (Game.IsKeyDown(crouchKey) && !proned)
                {
                    if (myChar.IsInAnyVehicle(false) == false)
                    {
                        crouch = !crouch;
                        if (crouch == false)
                        {
                            crouchC();
                        }
                    }
                }
                
                if (crouch == true)
                {
                    N.RequestAnimSet("move_ped_crouched");
                    while (N.HasAnimSetLoaded("move_ped_crouched") == false)
                    {
                        GameFiber.Wait(100);
                    }
                    N.SetPedMovementClipset(myChar, "move_ped_crouched", 1.0f);
                    N.SetPedWeaponMovementClipset(myChar, "move_ped_crouched", 1.0f);
                    N.SetPedStrafeClipset(myChar, "move_ped_crouched_strafing", 1.0f);
                    if (N.GetFollowPedCamViewMode<int>() == 4)
                    {
                        N.SetFollowPedCamViewMode(0);
                    }
                }
            }
        }

        public static void ProcessProne()
        {
            while (true)
            {
                GameFiber.Yield();
                GameFiber.Wait(1);
                Ped myChar = Game.LocalPlayer.Character;

                if (Game.IsKeyDown(proneKey) && !crouch)
                {
                    if (myChar.IsInAnyVehicle(false) == false)
                    {
                        proned = !proned;
                        if (proned == false)
                        {
                            proneC();
                        } 
                        else
                        {
                            myChar.Tasks.ClearImmediately();
                            N.RequestAnimSet("move_crawl");
                            while (N.HasAnimSetLoaded("move_crawl") == false)
                            {
                                GameFiber.Wait(100);
                            }
                            if (myChar.IsSprinting || myChar.IsRunning || myChar.Speed > 5f)
                            {
                                N.TaskPlayAnim(myChar, "move_jump", "dive_start_run", 8.0f, 1.0f, -1, 0, 0.0f, false, false, false);
                                GameFiber.Wait(1000);
                                myChar.Tasks.ClearImmediately();
                            }
                            N.TaskPlayAnim(myChar, "move_crawl", "onfront_fwd", 8f, -8f, -1, 2, 0.0f, false, false, false);
                            if (N.IsPedArmed<bool>(myChar, 4 | 2))
                            {
                                myChar.Tasks.Clear();
                                N.TaskAimGunScripted(myChar, N.GetHashKey<uint>("SCRIPTED_GUN_TASK_PLANE_WING"), true, true);
                            }
                        }
                    }
                }

                if (proned == true)
                {
                    ProneMovement();
                }
            }
        }
    }
}
