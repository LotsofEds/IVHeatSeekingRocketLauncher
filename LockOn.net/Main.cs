using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GTA;
using System.Windows.Forms;
using System.Drawing;
using GTA.Native;
using System.Net.Sockets;

namespace LockOn.net
{
    public class Main : Script
    {
        private GTA.Object rocket = null;
        private bool isAiming;
        private bool isLocking;
        private bool bLaunched;
        private bool inLineOfSight;
        private double timeWait;
        private Vector3 fixRotation;
        private Vector3 fixDirection;
        private Vector3 fixDirectionAux;
        private Vector3 initialPosition;
        private Int32 PTFXSmoke, PTFXExp;
        private Int32 LaunchSID, MoveSID, BeepSID;
        private Vehicle[] vehList;
        private Vehicle targetVeh = null;
        private Vehicle tmpTargetVeh = null;
        private double targetVehOffsetH;
        private Vector3 midPos;
        private Vector2 targetPosScrn;
        private double timeLocked = 0;
        private Texture tex1, tex2, tex3, tex4, tex5, tex6, tex7, tex8, tex9, tex11, tex12, tex13, tex14, tex15, tex16, tex17, tex18, tex19, texTarget;
        private bool bWasAlive = false;
        private bool bNormalMode = false;
        private double MaxAttackDistance;
        private double zOffSetPTFX = 0;
        private string rocketModel;
        private string rocketTrail;
        private double timeLockingBeep = 0;
        private double timeCheckRocket = 50;
        private double timeCheckSoundBug = 500;
        private double timepedCheck = 200;
        private double fuseTime = 2000;
        private int wantedLevel = 1;
        private GTA.Ped vped;
        private bool CheckLineOfSight;

        private string soundLocked = "";
        private string soundLocking = "";
        private System.Media.SoundPlayer sndJLaunch = new System.Media.SoundPlayer();
        private System.Media.SoundPlayer sndLocking = new System.Media.SoundPlayer();
        private System.Media.SoundPlayer sndLocked = new System.Media.SoundPlayer();
        public Main()
        {
            loadAux();
            this.Interval = 10;
            this.Tick += new EventHandler(this.general_tick);
            this.PerFrameDrawing += new GTA.GraphicsEventHandler(this.GraphicsEventHandler);
        }
        private void loadAux()
        {
            SettingsFile ini = SettingsFile.Open(Path.Combine("scripts", "LockOn.ini"));
            ini.Load();

            MoveSID = GTA.Native.Function.Call<Int32>("GET_SOUND_ID");
            LaunchSID = GTA.Native.Function.Call<Int32>("GET_SOUND_ID");
            BeepSID = GTA.Native.Function.Call<Int32>("GET_SOUND_ID");

            tex1 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim1.png"));
            tex2 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim2.png"));
            tex3 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim3.png"));
            tex4 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim4.png"));
            tex5 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim5.png"));
            tex6 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim6.png"));
            tex7 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim7.png"));
            tex8 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim8.png"));
            tex9 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim9.png"));
            tex11 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim11.png"));
            tex12 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim12.png"));
            tex13 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim13.png"));
            tex14 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim14.png"));
            tex15 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim15.png"));
            tex16 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim16.png"));
            tex17 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim17.png"));
            tex18 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim18.png"));
            tex19 = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllgreenAnim19.png"));
            texTarget = new Texture(File.ReadAllBytes(".\\scripts\\RocketLockFiles\\hudllredAnim.png"));

            CheckLineOfSight = ini.GetValueBool("CheckLineOfSight", "general", false);
            MaxAttackDistance = ini.GetValueFloat("MaxAttackDistance", "general", 200);
            rocketModel = ini.GetValueString("rocketModel", "general", "w_e3_rocket92");
            rocketTrail = ini.GetValueString("rocketTrail", "general", "weap_rocket_player");
            soundLocking = ini.GetValueString("soundLocking", "general", "");
            soundLocked = ini.GetValueString("soundLocked", "general", "");

            rocket = World.CreateObject(rocketModel, Player.Character.Position + Vector3.WorldUp * 10);
            if (!Exists(rocket))
                rocket = World.CreateObject(rocketModel, Player.Character.Position + Vector3.WorldUp * 10);

            zOffSetPTFX = rocket.Model.GetDimensions().Z * -1;

            rocket.Visible = false;
            rocket.AttachToPed(Player.Character, Bone.RightHand, new Vector3(0.62f, 0.04f, 0), new Vector3(0, 0, -1.55f));

            sndJLaunch.SoundLocation = ".\\scripts\\RocketLockFiles\\jlaunch.wav";
            sndJLaunch.Load();

            if (soundLocking != "")
            {
                sndLocking.SoundLocation = ".\\scripts\\RocketLockFiles\\" + soundLocking;
                sndLocking.Load();
            }
            if (soundLocked != "")
            {
                sndLocked.SoundLocation = ".\\scripts\\RocketLockFiles\\" + soundLocked;
                sndLocked.Load();
            }
        }
        private bool calcMidPos(bool bPos2 = false)
        {
            Vector3 tmpPos;
            Vector3 tmpVel = Vector3.Zero;

            if (Exists(targetVeh))
            {
                tmpPos = targetVeh.Position;
                tmpVel = targetVeh.Velocity;
            }
            else
                return false;

            midPos = initialPosition + (fixDirectionAux * (float)(tmpPos.DistanceTo(initialPosition) * 0.3));
            midPos.Z += 1;

            return true;
        }

        private void general_tick(object sender, EventArgs e)
        {
            if ((Player.Character.Weapons.Current == Weapon.Episodic_23) && (Game.isGameKeyPressed(GameKey.Aim)) && !isAiming && !bLaunched && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload") && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload_crouch") && (GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire") || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire_crouch")))
            {
                targetVeh = null;
                tmpTargetVeh = null;
                isAiming = true;
                rocket.Collision = true;
            }

            if ((Game.isGameKeyPressed(GameKey.Attack)) && (Player.Character.Weapons.Current == Weapon.Episodic_23) && !bLaunched && ((!GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload") && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload_crouch") && (GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire") || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire_crouch"))) || (Player.Character.isRagdoll)))
            {
                rocket.Collision = true;
                GTA.Native.Function.Call("SET_OBJECT_RECORDS_COLLISIONS", rocket, false);

                rocket.Detach();
                rocket.Visible = true;
                fixRotation = Game.CurrentCamera.Rotation;
                fixDirection = Game.CurrentCamera.Direction;
                fixDirectionAux = Player.Character.Direction;
                initialPosition = Player.Character.Position;
                bLaunched = true;
                fuseTime = 2000;
                timeCheckSoundBug = 500;

                GTA.Native.Function.Call("STOP_SOUND", LaunchSID);
                GTA.Native.Function.Call("PLAY_SOUND_FROM_OBJECT", LaunchSID, "ROCKET_GRENADE_LAUNCH", rocket);

                GTA.Native.Function.Call("STOP_PTFX", PTFXSmoke);
                PTFXSmoke = GTA.Native.Function.Call<Int32>("START_PTFX_ON_OBJ", rocketTrail, rocket, 0, 0, zOffSetPTFX, 0, 0, 0, 2.0);

                GTA.Native.Function.Call("STOP_SOUND", MoveSID);
                GTA.Native.Function.Call("PLAY_SOUND_FROM_OBJECT", MoveSID, "GENERAL_WEAPONS_ROCKET_LOOP", rocket);

                timeWait = 25;
                rocket.ApplyForce(fixDirection * 4);

                rocket.Rotation = fixRotation;

                bool bWanted = false;

                foreach (Ped p in World.GetPeds(Player.Character.Position, 20.0f))
                {
                    if (Exists(p) && (p != Player.Character))
                    {
                        if ((p.PedType != PedType.Cop))
                            p.Task.FleeFromChar(Player.Character, false, 15000);
                        else
                            bWanted = true;
                    }
                }

                if (bWanted)
                {
                    Player.Character.WantedByPolice = true;
                    GTA.Native.Function.Call("ALTER_WANTED_LEVEL_NO_DROP", Player, 1);
                    GTA.Native.Function.Call("APPLY_WANTED_LEVEL_CHANGE_NOW", Player, 1);
                }
                if (timeLocked >= 1000)
                    calcMidPos();
            }

            if (timeCheckSoundBug <= 0)
            {
                timeCheckSoundBug = 500;
            }
            else
                timeCheckSoundBug -= this.Interval;

            if ((Player.Character.Weapons.Current == Weapon.Episodic_23) && Player.Character.isShooting)
            {
                foreach (GTA.Object o in World.GetAllObjects("w_e3_rocket92"))
                {
                    if (Exists(o) && (o != rocket))
                        o.Delete();
                }
            }

            if (fuseTime <= 0 && bLaunched)
                fuseTime = 2000;

            else if (bLaunched)
                fuseTime -= this.Interval;

            if (bLaunched && timeLocked >= 1000)
            {
                if ((GTA.Native.Function.Call<bool>("HAS_OBJECT_COLLIDED_WITH_ANYTHING", rocket) || fuseTime <= 0 || rocket.Position.DistanceTo(targetVeh.Position) < 0.5f || (rocket.Position.DistanceTo(Player.Character.Position) > 200)))
                {
                    bool bWanted = false;

                    foreach (Ped p in World.GetPeds(rocket.Position, 30.0f))
                    {
                        if (Exists(p))
                        {
                            if (p.PedType == PedType.Cop)
                                bWanted = true;
                        }
                    }

                    if (bWanted)
                    {
                        wantedLevel += 1;
                        Player.Character.WantedByPolice = true;
                        GTA.Native.Function.Call("ALTER_WANTED_LEVEL_NO_DROP", Player, wantedLevel);
                        GTA.Native.Function.Call("APPLY_WANTED_LEVEL_CHANGE_NOW", Player, wantedLevel);
                    }

                    GTA.Native.Function.Call("STOP_PTFX", PTFXExp);

                    GTA.Native.Function.Call("STOP_SOUND", MoveSID);
                    GTA.Native.Function.Call("PLAY_SOUND_FROM_POSITION", MoveSID, "PAYPHONE_INSERT_COIN", rocket.Position.X, rocket.Position.Y, (rocket.Position.Z + 100f));

                    World.AddExplosion(rocket.Position, ExplosionType.Rocket, 100f, true, true, 1.0f);

                    PTFXExp = GTA.Native.Function.Call<Int32>("START_PTFX", "exp_rocket", rocket.Position.X, rocket.Position.Y, rocket.Position.Z, 0, 0, 0, 1.0);

                    resetRocket();
                    bLaunched = false;
                }
                else
                {
                    Vector3 tmpDir = Vector3.Zero;
                    Vector3 tmpPos = Vector3.Zero;

                    if (Exists(targetVeh))
                    {
                        tmpDir = Vector3.Normalize(targetVeh.Position + Vector3.WorldUp * (float)targetVehOffsetH - rocket.Position);

                        rocket.ApplyForce(tmpDir * 15);

                        tmpPos = targetVeh.Position;
                    }

                    if (timeWait <= 0 && rocket.Position.DistanceTo(Player.Character.Position) > 1.0f)
                        GTA.Native.Function.Call("SET_OBJECT_RECORDS_COLLISIONS", rocket, true);
                    else
                        timeWait -= this.Interval;

                    Int16 tmpDiv;

                    if (rocket.Position.DistanceTo(tmpPos) > 3)
                        tmpDiv = 10;
                    else
                        tmpDiv = 50;

                    Vector3 tmpRot = Helper.DirectionToRotation(tmpDir, 0);
                    float incValue = Math.Abs(fixRotation.X - tmpRot.X) / (float)tmpDiv;

                    if (fixRotation.X > tmpRot.X)
                        fixRotation.X -= incValue;
                    else if (fixRotation.X < tmpRot.X)
                        fixRotation.X += incValue;
                    incValue = Math.Abs(fixRotation.Y - tmpRot.Y) / (float)tmpDiv;

                    if (fixRotation.Y > tmpRot.Y)
                        fixRotation.Y -= incValue;
                    else if (fixRotation.Y < tmpRot.Y)
                        fixRotation.Y += incValue;
                    fixRotation = Helper.DirectionToRotation(fixDirection, 0);

                    rocket.Rotation = fixRotation;
                }
            }
            else if (bLaunched && timeLocked < 1000)
            {
                if ((GTA.Native.Function.Call<bool>("HAS_OBJECT_COLLIDED_WITH_ANYTHING", rocket) || fuseTime <= 0 || (rocket.Position.DistanceTo(Player.Character.Position) > 200)))
                {
                    bool bWanted = false;

                    foreach (Ped p in World.GetPeds(rocket.Position, 30.0f))
                    {
                        if (Exists(p))
                        {
                            if (p.PedType == PedType.Cop)
                                bWanted = true;
                        }
                    }

                    if (bWanted)
                    {
                        wantedLevel += 1;
                        Player.Character.WantedByPolice = true;
                        GTA.Native.Function.Call("ALTER_WANTED_LEVEL_NO_DROP", Player, wantedLevel);
                        GTA.Native.Function.Call("APPLY_WANTED_LEVEL_CHANGE_NOW", Player, wantedLevel);
                    }

                    GTA.Native.Function.Call("STOP_PTFX", PTFXExp);

                    GTA.Native.Function.Call("STOP_SOUND", MoveSID);
                    GTA.Native.Function.Call("PLAY_SOUND_FROM_POSITION", MoveSID, "PAYPHONE_INSERT_COIN", rocket.Position.X, rocket.Position.Y, (rocket.Position.Z + 100f));

                    World.AddExplosion(rocket.Position, ExplosionType.Rocket, 100f, true, true, 1.0f);

                    PTFXExp = GTA.Native.Function.Call<Int32>("START_PTFX", "exp_rocket", rocket.Position.X, rocket.Position.Y, rocket.Position.Z, 0, 0, 0, 1.0);

                    resetRocket();
                    bLaunched = false;
                }
                else
                {
                    rocket.ApplyForce(fixDirection * 15);

                    if (timeWait <= 0 && rocket.Position.DistanceTo(Player.Character.Position) > 1.0f)
                        GTA.Native.Function.Call("SET_OBJECT_RECORDS_COLLISIONS", rocket, true);
                    else
                        timeWait -= this.Interval;

                    rocket.Rotation = fixRotation;
                }
            }
            else
            {
                rocket.Visible = false;

                if (timeCheckRocket <= 0)
                {
                    timeCheckRocket = 50;

                    if ((rocket.Position.DistanceTo(Player.Character.Position) > 2 || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload") || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload_crouch")) && !isAiming)
                        resetRocket();
                }
                else
                    timeCheckRocket -= this.Interval;
            }

            if (!Game.isGameKeyPressed(GameKey.Aim) && !bLaunched && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire") && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire_crouch") || (GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload") || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload_crouch")))
            {
                vehList = null;
                isAiming = false;
                timeLocked = 0;
            }

            else if ((Player.Character.Weapons.Current == Weapon.Episodic_23) && !Player.Character.isGettingUp)
            {
                if (!bLaunched)
                {
                    if (vehList == null)
                        vehList = World.GetVehicles(Player.Character.GetBonePosition(Bone.Head) + Game.CurrentCamera.Direction * (float)(MaxAttackDistance / 2), (float)MaxAttackDistance);

                    if (Exists(vehList))
                    {
                        if (Exists(tmpTargetVeh) && tmpTargetVeh.Position.DistanceTo(Player.Character.Position + Game.CurrentCamera.Direction * tmpTargetVeh.Position.DistanceTo(Player.Character.Position)) > 4)
                        {
                            targetVeh = null;
                            tmpTargetVeh = null;
                            timeLocked = 0;
                            isLocking = true;
                        }

                        if (!Exists(tmpTargetVeh))
                        {
                            foreach (Vehicle v in vehList)
                            {
                                if (Exists(v))
                                {
                                    if (v.isOnScreen && !GTA.Native.Function.Call<bool>("IS_CAR_DEAD", v) && (v.Position.DistanceTo(Player.Character.Position) > 2) && (v.Position.DistanceTo(Player.Character.Position + Game.CurrentCamera.Direction * v.Position.DistanceTo(Player.Character.Position)) < 4))
                                    {
                                        tmpTargetVeh = v;
                                        timeLocked = 0;
                                        targetVehOffsetH = v.Model.GetDimensions().Z / (double)4;
                                        break;
                                    }
                                }
                            }
                        }

                        if (Exists(tmpTargetVeh))
                        {
                            if (CheckLineOfSight)
                            {
                                if (!Exists(vped))
                                {
                                    timepedCheck = 100;
                                    vped = World.CreatePed("m_y_thief", Player.Character.Position + Vector3.WorldUp * 10);
                                    vped.Visible = false;
                                }
                                else if (Exists(vped) && !vped.isAttachedToVehicle())
                                    vped.AttachTo(tmpTargetVeh, new Vector3(0, 0, 0));
                                else if (Exists(vped) && vped.isAttachedToVehicle())
                                {
                                    if (timepedCheck <= 0 && (GTA.Native.Function.Call<bool>("HAS_CHAR_SPOTTED_CHAR", Player.Character, vped)))
                                    {
                                        inLineOfSight = true;
                                        vped.Delete();
                                    }
                                    else if (timepedCheck <= 0 && (!GTA.Native.Function.Call<bool>("HAS_CHAR_SPOTTED_CHAR", Player.Character, vped)))
                                    {
                                        inLineOfSight = false;
                                        vped.Delete();
                                    }
                                    else
                                        timepedCheck -= this.Interval;
                                }
                            }
                            if (timeLocked > 1000 && (inLineOfSight || !CheckLineOfSight))
                            {
                                targetVeh = tmpTargetVeh;

                                if (soundLocked == "")
                                {
                                    GTA.Native.Function.Call("STOP_SOUND", BeepSID);
                                    GTA.Native.Function.Call("PLAY_SOUND_FROM_PED", BeepSID, "GENERAL_FRONTEND_GAME_ELECTRIC_ALARM", Player.Character);
                                }
                                else
                                {
                                    sndLocked.Stop();
                                    sndLocked.Play();
                                }
                            }
                            else if ((inLineOfSight || !CheckLineOfSight))
                            {
                                timeLocked += this.Interval;

                                if ((soundLocking != ""))
                                {
                                    if (timeLockingBeep <= 0)
                                    {
                                        timeLockingBeep = 150;
                                        sndLocking.Play();
                                    }
                                    else
                                        timeLockingBeep -= this.Interval;
                                }
                            }
                            else
                                timeLocked = 0;
                        }
                        else
                        {
                            isLocking = false;
                            targetVeh = null;
                            timeLocked = 0;
                        }
                    }
                }
            }
        }

        private void drawSpriteAux(GTA.GraphicsEventArgs e, Texture tex, double x, double y, double w, double h, double rot, Color col, bool square = false)
        {
            if (square)
                e.Graphics.DrawSprite(tex, (float)(x), (float)(y), (float)(w), (float)(h), (float)rot, col);
            else
                e.Graphics.DrawSprite(tex, (float)(x), (float)(y), (float)(w), (float)(h), (float)rot, col);
        }

        private void GraphicsEventHandler(object sender, GTA.GraphicsEventArgs e)
        {
            if (Game.isGameKeyPressed(GameKey.Aim) && Player.Character.isAlive && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload") && !GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "reload_crouch") && (GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire") || GTA.Native.Function.Call<bool>("IS_CHAR_PLAYING_ANIM", Game.LocalPlayer.Character, "gun@rocket", "fire_crouch")))
            {
                if (Exists(tmpTargetVeh) || Exists(targetVeh))
                {
                    Vector3 pos;
                    Color tmpColor;

                    if (timeLocked >= 1000)
                    {
                        tmpColor = Color.FromArgb(100, 255, 0, 0);
                    }
                    else if (timeLocked > 0)
                        tmpColor = Color.FromArgb(100, 255, 255, 255);

                    if (Exists(targetVeh))
                        pos = targetVeh.Position;
                    else if (Exists(tmpTargetVeh))
                        pos = tmpTargetVeh.Position;
                    else
                        pos = Vector3.Zero;

                    targetPosScrn = CoordToScreen(pos);


                    if (timeLocked >= 1000)
                    {
                        drawSpriteAux(e, texTarget, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 985)
                    {
                        drawSpriteAux(e, tex19, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 970)
                    {
                        drawSpriteAux(e, tex18, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 955)
                    {
                        drawSpriteAux(e, tex17, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 940)
                    {
                        drawSpriteAux(e, tex16, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 925)
                    {
                        drawSpriteAux(e, tex15, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 910)
                    {
                        drawSpriteAux(e, tex14, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 895)
                    {
                        drawSpriteAux(e, tex13, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 870)
                    {
                        drawSpriteAux(e, tex12, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 855)
                    {
                        drawSpriteAux(e, tex11, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 840)
                    {
                        drawSpriteAux(e, tex9, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 825)
                    {
                        drawSpriteAux(e, tex8, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 810)
                    {
                        drawSpriteAux(e, tex7, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 795)
                    {
                        drawSpriteAux(e, tex6, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 780)
                    {
                        drawSpriteAux(e, tex5, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 765)
                    {
                        drawSpriteAux(e, tex4, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 750)
                    {
                        drawSpriteAux(e, tex3, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 735)
                    {
                        drawSpriteAux(e, tex2, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                    else if (timeLocked > 0)
                    {
                        drawSpriteAux(e, tex1, targetPosScrn.X, targetPosScrn.Y, Game.Resolution.Height * 0.15, Game.Resolution.Height * 0.15, 0, Color.White);
                    }
                }
            }
        }

        private Vector2 CoordToScreen(Vector3 posOn3D)
        {
            Vector2 screen = new Vector2(0.0f, 0.0f); ;
            GTA.Native.Pointer x = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer y = new GTA.Native.Pointer(typeof(float));
            Vector3 pos = posOn3D;
            GTA.Native.Pointer pointer = new GTA.Native.Pointer(typeof(int));
            Function.Call("GET_GAME_VIEWPORT_ID", pointer);
            int num = (int)pointer;
            Function.Call("GET_VIEWPORT_POSITION_OF_COORD", pos.X, pos.Y, pos.Z, num, x, y);
            screen = new Vector2((float)x, (float)y);
            return screen;
        }

        private void resetRocket()
        {
            GTA.Native.Function.Call("STOP_PTFX", PTFXSmoke);
            GTA.Native.Function.Call("STOP_SOUND", MoveSID);
            rocket.Collision = false;
            rocket.Visible = false;
            inLineOfSight = false;

            GTA.Native.Function.Call("SET_OBJECT_RECORDS_COLLISIONS", rocket, false);
            rocket.AttachToPed(Player.Character, Bone.RightHand, new Vector3(0.62f, 0.04f, 0), new Vector3(0, 0, -1.55f));

            vehList = null;
            targetVeh = null;
            tmpTargetVeh = null;
        }
    }
}