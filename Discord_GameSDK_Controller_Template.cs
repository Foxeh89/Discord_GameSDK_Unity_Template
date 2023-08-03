using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Discord;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class Discord_GameSDK_Controller_Template : MonoBehaviour
{
	public static Discord_GameSDK_Controller Instance; //made an instance since i want it optionally running at start of my game
	public GameObject DiscordPrefab; //just a prefab of itself incase you ever wanted to restart the discord script.
    public Discord.Discord discord;
    public long applicationID;//the app id you made on https://discord.com/developers/applications
    [Space]
    public ActivityManager activityManager; //all rich presence and status stuff
    public UserManager userManager; //optional information like discord name and avatar
    public string details; //what the player is currently doing 
    public string state; //user's current party status
	public ScriptStates scriptState;
    public enum ScriptStates
    {
        Inactive = 0,
        Booting = 1,
        Active = 2,
        Stopping = 3,
        Leaving = 4 //probably wont be used, by thte time leaving is called, it's already trying to delete itself.
    }
    
#region rich presence image variables
    [Space]
    public string largeImage;
    public string largeText;
    public string smallImage;
    public string smallText;
	#endregion

#region discord user variables
	//if you ever wanted to get your own user information, you can use these variables
    [Space]
    public User currentUser;
    public Int64 userId;
    public string username;
    public string discriminator;
    public string avatar;
    public bool bot; //not used in this script yet
	
	#endregion
	
	public SpriteRenderer avatarSpriterenderer;
    public Image avatarImage;
    public string avatarUrl;

#region party information variables
    [Space]

    public string partyId; //the ID of the player's party, lobby or group. must match the host if you want their user information shown on join
    public int partySize;
    public int partyMax;
    public string joinSecret; //unique hashed string for chat invitations and ask to join. 
    private string HostJoinSecret; //just the host's join secret. they must match if clients want to invite people to host's room alongside the party id

    [Space] //other stuff i dont know yet
    public string matchSecret; //unique hash for the given match context
    public string spectateSecret; //unique hash for Spectate button. not going to use
	
	#endregion
	
#region time variables
    public long time; //time value to get, will switch between launch and game time
	
    public long launchTime; //Optional: time since app launched. i normally use this as my default
    public long gameTime; //Optional: i used this for just having a game duration.

#endregion



#region useful extra variables
	//all of this is optional. feel free to remove it if you dont have a use for it.
    public string menuSceneName; //Replace string name checks with enums because they are a lot cheaper
    public string multiplayerSceneName; //Replace string name checks with enums because they are a lot cheaper
    private bool inGame;

    public float eta; //just a countdown i used for when i wanted to restart my discord status. not necessary anymore though
    private float timer = 6f;

    public Text pathText; //make a text field if you really want to show the string path of your game.exe.
    public string _filePath;//the location this game is being ran on. you will register this to discord so the discord can launch your app if it's closed when you hit join

    public Coroutine coroutine; //just a handler operation
    public Coroutine codeCoroutine; //coroutine to get informat

#endregion

    void Awake() 
    {
        scriptState = ScriptStates.Inactive; //setting as inactive so update loops dont happen before everything else is done

        if (Instance == null) //if there's no controller made, assign this one
        {
            Instance = this;
        }
        else
        {
            print("there's a discord script here already. lets destroy ours");
            Destroy(gameObject);
            return;
        }
        

        DontDestroyOnLoad(gameObject);
        
    }

    void Start()
    {
        scriptState = ScriptStates.Booting;
        InitializeDiscord();
    }
	
	void Update()
    {
        //here's an example of how you can just check what scene you're in and just run that method in update.
		Scene scene = SceneManager.GetActiveScene(); //im just getting the active scene because i like changing the status message based on the scene im in.

		//runs one of the voids depending on what scene is active.
        if (Application.isPlaying)
        {
            if (scene.name == menuSceneName && scene.name != multiplayerSceneName)
            {
                //print("in menus");
                SetValuesToMenu();
            }
            if (scene.name == multiplayerSceneName && scene.name != menuSceneName)
            {
                //print("in multiplayer");
                SetValuesToMultiplayerRoom();
            }
            if (scene.name != menuSceneName && scene.name != multiplayerSceneName)
            {
                //print("in stage");
                SetValues();
            }
            
        }

        //We only want RunCallbacks happening after discord has started, to prevent errors from happening.
        if (scriptState != ScriptStates.Inactive)
        {
            // Destroy the GameObject if Discord isn't running
            try
            {
                discord.RunCallbacks(); //RunCallbacks is basically just listening to responses from discord. it's necessary for things like OnActivityJoin and etc.
            }
            catch
            {
                print("Discord isn't running. destroy");
                Destroy(gameObject);
            }
        }
        
    }

    void LateUpdate()
    {
		//as long as the scriptstate is active, we can update the discord status
		if (scriptState == ScriptStates.Active)
				UpdateStatus();
    }
	
	
	#region voids to modify status message
	/// <summary>
    /// These are just some suggestional templates for having your status messages work.
    /// </summary>
    public void SetValuesToMenu()
    {
        state = "idle";
        details = "in game menus";
        largeImage = "Picture_of_menu";
        largeText = "Main Menu";
        smallImage = null; //probably character portrait
        smallText = null; //probably the character's name
        time = launchTime;
        gameTime = 0; //not in a stage, so lets set it back to 0 for consistency

        if (partyId != null) //since this void is about going through menus offline, lets just clear the online dependent variables here.
        {
            partyId = null;
            partySize = 0;
            partyMax = 0;
            matchSecret = null;
            joinSecret = null;
            spectateSecret = null;
        }
        
    }

    //in room
    public void SetValuesToMultiplayerRoom()
    {
        state = "Selecting character";
        details = "In Character Select";
        largeImage = "Character_Select_screen_picture";
        largeText = "what people will see when they highlight the large picture";
        //smallImage = "";
        //smallText = "";
        time = launchTime;
        gameTime = 0;
		
		//checking if multiplayer is local or online to change the discord status to reflect that.
		bool YouAreHost; //just a placeholder bool. come up with your own host check

        //SERVER HOST
        if (YouAreHost == true) 
        {
            partyId = YourRoomCode;
            partySize = NumberOfPlayersInParty; //never letPartySize be 0 or less
            partyMax = MaximumAllowedPlayers;
			joinSecret = CodeForJoiningGame; 
			matchSecret = "foo match"; // Unused and no idea what it's for. Optional
			
        }
        //CLIENT
        if (YouAreHost == false) //i mean if you'r not the host, then you're probably a client
        {
            //information needs to match the host's party info
            partyId = HostPartyId;
            partySize = NumberOfPlayersInParty; //never letPartySize be 0 or less. otherwise it wont display.
            partyMax = MaximumAllowedPlayers;
            joinSecret = HostJoinSecret; //grabbing the hosts to also invite people to the same room.
        }

        
        //if Offline. dont know how you got in a online multiplayer room while offline. unless it's the same scene i guess. here for optional consistency
        if(!BoltNetwork.isConnected)
        {
            partyId = null;
            partySize = 0;
            partyMax = 0;
            joinSecret = null;
            matchSecret = null;
        }
        

        //spectateSecret = null;
    }

    //This could just be one for actually in stages. of course you can make extra voids for these cases
    public void SetValues()
    {
		//strings for status message information. test on the Rich Presense Visualizer from the website https://discord.com/developers/applications
        details = "Stage_Name";
        largeImage = "Stage_Picture";
        largeText = "Stage_Description";
        smallImage = "Character_Image";
        smallText = "Character_Name";

		//make sure you call your ingame time only once, otherwise your time is gonna jump everywhere exampe: if(gameTime == 0)
        TimeSpan timeSpan = GameManager.Stage.ActiveTime; //assigning a new timespan variable
		DateTime dt = DateTime.SpecifyKind(DateTime.UtcNow + timeSpan, DateTimeKind.Utc); //getting time for right now
        gameTime = new DateTimeOffset(dt).ToUnixTimeMilliseconds(); //converting the time to epoch time
        time = gameTime;

        if (PlayingOnline)
        {
            state = GameManager.Instance.LevelData.GameMode; //the state is usually a good spot to describe something like the game mode
            //SERVER HOST
            if (YouAreHost == true)
            {
                partyId = YourRoomCode;
				partySize = NumberOfPlayersInParty; //never letPartySize be 0 or less
				partyMax = MaximumAllowedPlayers;
				joinSecret = CodeForJoiningGame; 
				matchSecret = "foo match"; // Unused and no idea what it's for. Optional
                
            }
            //CLIENT
            if (!YouAreHost) //if you're online as a client
            {
				//information needs to match the host's party info
				partyId = HostPartyId;
				partySize = NumberOfPlayersInParty; //never letPartySize be 0 or less
				partyMax = MaximumAllowedPlayers;
				joinSecret = HostJoinSecret; //grabbing the hosts to also invite people to the same room.
            }
            //spectateSecret = null; //still dont know what to do with this
        }
        //offline play
        if(PlayingOnline == false)
        {
            if (Game.IsPlayingDemo)
                state = "Watching Demo";
            else
            {
                state = "Single/local Player";
            }
        }
        
    }
	#endregion
	
	#region Discord Actions
	//run this once to get discord started up
	public void CreateActivity()
    {
        activity = new Discord.Activity
        {
            State = state, //what are they doing
            Details = details, //game mode?

            Assets = //the images
                {
                    LargeImage = largeImage, //large image asset value
                    LargeText = largeText, //large image tooltip
                    SmallImage = smallImage, //small image asset value
                    SmallText = smallText, //small image tooltip
                },
            Timestamps =
                {
                    Start = time //remove if you dont want to show time
                    //End = 0000 //if you include end, then it will be a countdown.
                },
            Party = //things to allow for joining eachother
                {
                    Id = partyId,
                    Size =
                    {
                        CurrentSize = partySize, //current size of party
                        MaxSize = partyMax, //maximum party size
                    },
                },
            Secrets = //what does secrets mean?
                {
                    Match = matchSecret,
                    Join = joinSecret, 
                    //Spectate = spectateSecret,
                }
        };

        activityManager.UpdateActivity(activity, (res) =>
        {
            //give warning if failed to connect to discord
            if (res != Discord.Result.Ok)
            {
                discord.SetLogHook(Discord.LogLevel.Debug, LogProblemsFunction);
                Debug.LogWarning("Failed connecting to Discord!");
                RefreshDiscord(); //i like to start over if the script goes wrong somewhere.
            }
        });
    }
	
	public void GetCurrentUser()
    {
        ///<summary>
        ///gets your discord user information. up to you to figure out what you want to do with this information.
        ///can probably run this outside of update
        ///</summary>
        userManager.OnCurrentUserUpdate += () =>
        {
            currentUser = userManager.GetCurrentUser();
            userId = userManager.GetCurrentUser().Id;
            username = userManager.GetCurrentUser().Username;
            discriminator = userManager.GetCurrentUser().Discriminator;
            avatar = userManager.GetCurrentUser().Avatar;

            //avatarUrl = "https://cdn.discordapp.com/avatars/" + userId + "/" + avatar + ".png"; //probably better than checking the string if it's empty
        };
    }
	
	
    public void GetUserAvatar(long _userid) 
    {
		//downloads YOUR discord avatar and displays it on screen.... UPSIDE DOWN 
		//if you have a better way to display the avatar, feel free to let me know.
		//will need to make a new method for geting another person's avatar and name.
        print("getting avatar");
        var _handle = new Discord.ImageHandle()
        {
            Id = _userid,
            Size = 1024
        };

        ImageManager imageManager;
        imageManager = discord.GetImageManager();
        imageManager.Fetch(_handle, false, (result, handle) =>
        {
            if (result == Discord.Result.Ok)
            {
                var texture = imageManager.GetTexture(handle);
                Sprite sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, -texture.height), new Vector2(0.5f, 0.5f));
                avatarSpriterenderer.sprite = sprite;
                avatarSpriterenderer.flipY = true; //gotta manually flip this because even if we create the texture to be upside down, it wont.
            }
        });
    }
	
	
	//run once at startup after discord is booted up.
	public void SetOnJoinAcitivy() 
    {
        // Received when someone accepts a request to join or invite.
        // Use secrets to receive back the information needed to add the user to the group/party/match
        // this is for the person who actually presses the join button.
        // after this is set, it will wait on discord.RunCallbacks(); to get the response.
		// DO NOT put this under Update/LateUpdate methods because it will give your garbage collection issues, which causes bad performance
        activityManager.OnActivityJoin += secret =>
        {
            print("You have pressed discord's join button and OnActivityJoin was called. OnJoin {0}", secret);
            HostJoinSecret = secret; //i like to make the join secret match the host's. you dont have to do that if you don't want to.
            
			//i suggest picking a scene where you don't mind the game doing what it needs to join game
			//this is just how i do it currently. feel free to replace this with your method
            switch (GameManager.Instance.activeScene)
            {
                case GameManager.ScenesEnum.Menu: //i join the game if im in the main menu
                    JoinGame(secret); //a coroutine that runs once.
                    break;
                case GameManager.ScenesEnum.InLevel: //if im not in the main menu, i go back to the main menu before joining.
                    if (GameManager.Instance.Demo.InDemo)
                    {
                        GameManager.Instance.Demo.ReturnToMenu();
                        GameManager.Instance.CharacterData = NetworkLobby.instance.DefaultCharacter;
                    }
                    else
                    {
                        SoundManager.StopActiveMusic();
                        GameManager.Instance.ToMenu();
                    }
                    break;
                case GameManager.ScenesEnum.MuliplayerLobby: //if you're already in a lobby, ¯\_(ツ)_/¯ maybe make a method/coroutine to leave that one and join a new one?
                    break;
            }
        };
    }
	
	public void SetOnActivityJoinRequest()
    {
		//waits for callbacks before it shows
        // A join request has been received. Consider Rendering the request on the UI.
        activityManager.OnActivityJoinRequest += (ref Discord.User user) =>
        {
            Debug.LogFormat("OnJoinRequest {0} {1}", user.Username, user.Id);
        };
    }
	
	/// <summary>
    /// Sends a game invite to a given user. If you do not have a valid activity with all the required fields, this call will error.
    /// Returns a Discord.Result callback.
	/// i never used this one yet so i'll update this with more details one day
    /// </summary>
    public void SendInvite(long _userId)
    {
        //var userId = 53908232506183680;
        activityManager.SendInvite(_userId, Discord.ActivityActionType.Join, "Come play!", (result) =>
        {
            if (result == Discord.Result.Ok)
            {
                Debug.Log("Success!: " + result);
            }
            else
            {
                Debug.LogWarning("Failed: " + result);
            }
        });
    }
	
	public void AcceptInvite(Discord.User _user)
    {
        //This function will accept an invite... haven't used this more, but it does get called when you hit join
        activityManager.AcceptInvite(_user.Id, result =>
        {
            Console.WriteLine("AcceptInvite {0}", result);
        });
    }
	
	
	//1. this will start up discord with the necessary things. treat it like Start()
    void InitializeDiscord()
    {
        // Log in with the Application ID
        discord = new Discord.Discord(applicationID, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);

        activityManager = discord.GetActivityManager();
        userManager = discord.GetUserManager();
        CreateActivity(); //moved create activity out of update.

        _filePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName; //gets filename, extension and directory all the way to the .exe file
        activityManager.RegisterCommand($"\"{_filePath}\""); //gets the game's full data path to launch the game.
        //pathText.text = _filePath; //debug for if you wanna see if the path is correct.
        if(launchTime == 0) //just to cover the launchtime if coming in from a refresh.
        launchTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(); //gets the current time this variable is called.

        UpdateStatus(); //might not need this on the initialize. but it's not hurting running once here.

        avatarUrl = "https://cdn.discordapp.com/avatars/" + userId + "/" + avatar + ".png"; //probably better than checking the string if it's empty

        eta = timer;


        //set script to active and start up the other features
        scriptState = ScriptStates.Active;
        SetOnActivityJoin();
        SetOnActivityJoinRequest();
        GetCurrentUser();
    }
	
	//2. run main loop
    void UpdateStatus()
    {
        // UpdateActivity on every frame, you could probably set it to run only when things change. 
        // BUT RunCallbacks needs to be every frame. that's why we keep it in Update

        try
        {
			//optional
			//personally i keep the script running for a time period before i choose it to automatically restart itself.
			//just incase it still gives me garbage collection errors, but it should be fine without it at this point.
            //countdown to clear activity test
            if (eta > 0f)
            {
                #region update activity
                //update activity

                activity.State = state;
                activity.Details = details;
                activity.Assets.LargeImage = largeImage;
                activity.Assets.LargeText = largeText;
                activity.Assets.SmallImage = smallImage;
                activity.Assets.SmallText = smallText;
                activity.Timestamps.Start = time;
                //activity.Timestamps.End = endtime;
                activity.Party.Id = partyId;
                activity.Party.Size.CurrentSize = partySize;
                activity.Party.Size.MaxSize = partyMax;
                //activity.Secrets.Match = matchSecret;
                activity.Secrets.Join = joinSecret;
                //activity.Secrets.Spectate = spectateSecret;

                #endregion

                //UpdateActivity, alongside RunCallbacks, are the 2 most important things to get and send information to discord.
                activityManager.UpdateActivity(activity, (res) =>
                {
                    //give warning if failed to connect to discord
                    if (res != Discord.Result.Ok)
                    {
                        discord.SetLogHook(Discord.LogLevel.Debug, LogProblemsFunction);
                        Debug.LogWarning("Failed connecting to Discord!");
                    }
                });

                //countdown
                eta -= Time.deltaTime;
            }
            if (eta < 0f) 
            {
                RefreshDiscord();
            }
        }
        catch
        {
            discord.SetLogHook(Discord.LogLevel.Debug, LogProblemsFunction);
            // If updating the status fails, we could restart the script by running RefreshDiscord
            print("updating status failed.");
            //Destroy(gameObject);
        }
    }
	
	//3. stop discord. should run only once
    void RefreshDiscord()
    {
        //set to stopping
        scriptState = ScriptStates.Stopping;
        print("RefreshDiscord");
        ClearDiscordActivity();
    }
	
	public void ClearDiscordActivity()
    {
        print("ClearDiscordActitivy is being called. waiting on the next callback update for response.");
        activityManager.ClearActivity((result) =>
        {
            if (result == Discord.Result.Ok)
            {
                Debug.LogFormat("Success! Result: " + result);

                Instance = null; //remove the instance
                var newDiscord = Instantiate(DiscordPrefab);
                newDiscord.name = gameObject.name; //we might as well take the name since we're replacing it
                newDiscord.GetComponent<Discord_Controller>().launchTime = launchTime; //synching the clock before leaving.
                scriptState = ScriptStates.Leaving;

                Destroy(gameObject); //we can destroy this one since the other is instantiated
            }
            else
            {
                Debug.LogFormat("Failed, Result: " + result);
                print("trying again");
                ClearDiscordActivity(); //gonna just try again. sometimes it just works after the first try. will update with a more solid reason later
                //print("destroying on fail");
                //Destroy(gameObject);
            }
        });
    }
	
	//supposedly gives you detailed error messages. havent fully tested this one yet.
	public void LogProblemsFunction(Discord.LogLevel level, string message)
    {
        Debug.LogFormat("Discord:{0} - {1}", level, message);
    }
	
	#endregion
	
	//4. This will clean your discord activity even in unity when you leave play mode. 
	//trying to run dispose anywhere else will just cause unity to crash.
    public void OnDestroy()
    {
        if(scriptState != ScriptStates.Inactive) //no need to dispose if it never made it past inactive.
        {
            print("OnDestroy was called, lets use dispose to clear activity");
            discord.Dispose();
        }
    }
	
	
	
	#region Join game coroutine
	
	private bool IsCoroutineActive; //the coroutine i do to let someone join my game.
    public void JoinGame(string secret)
    {
        if (IsCoroutineActive == false)
        {
            if (coroutine == null)
            {
                coroutine = StartCoroutine(JoinGameCoroutine(secret));
            }
        }
        
    }
    IEnumerator JoinGameCoroutine(string secret)
    {
        IsCoroutineActive = true;
        print("You have pressed discord's join button and the secret is " + secret);
		//do some stuff here that will put you in that player's game. exampe: connecting to the ip and party and scene
        yield return null;
    }
	
	
	#endregion
	
}