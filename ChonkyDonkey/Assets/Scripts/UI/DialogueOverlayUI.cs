using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueOverlayUI : MonoBehaviour
{
    [Header("Links")]
    public Image CharacterIcon;
    public Animator Controller;
    public TextMeshProUGUI NameLabel;
    public TypewriterText Typewriter;
    public GameObject NextArrow;
    public GameObject AwooButton;
    public GameObject InviteButton;
    public AudioSource BarkSource;
    
    [Header("Assets")]
    public CharacterDialogueSpriteCollection[] CharacterSprites;

    [Header("Config")] 
    public float TypewriterSpeed;
    public float TypewriterDelay;
    
    // apparently, the new input system's "pressed" doesn't work like the old one, so tracking the pressed state here
    private bool inputFlag;

    private PlayerController cachedPlayer;

    // animator triggers
    private static readonly int IsShowing = Animator.StringToHash("IsShowing");
    private static readonly int CharacterChanged = Animator.StringToHash("CharacterChanged");
    private static readonly int OptionsShowing = Animator.StringToHash("OptionsShowing");

    //Affinity Bar
    private AffinityBar affinityBarScript;

    private PetId prevPet;

    private void Awake()
    {
        affinityBarScript = FindObjectOfType<AffinityBar>();
    }


    public void OnGreetDog(PetId dogId)
    {
        int affinity = affinityBarScript.ShowThisDogsAffinity((int)dogId);
        AudioClip bark = GetSprites(dogId).Bark;
        if (bark != null)
        {
            SFXHelper.PlaySound(bark);
        }
        OnTalk(dogId, affinity, DogReactionType.Greeting);
    }
    
    public void OnSuccess(PetId dogId, int affinity)
    {
        OnTalk(dogId, affinity, DogReactionType.Happy);
    }
    
    public void OnFail(PetId dogId, int affinity)
    {
        OnTalk(dogId, affinity, DogReactionType.Angry);
    }

    private void OnTalk(PetId pet, int affinity, DogReactionType reactionType)
    {
        ModeManager.Instance.ChangeMode(GameMode.Dialogue);
        
        // ReSharper disable once Unity.NoNullCoalescing
        cachedPlayer = cachedPlayer ?? FindObjectOfType<PlayerController>();

        int dogId = (int)pet;

        // load the dialogue text
        var dog = StatsLoader.Get(dogId);

        // play the show animation
        Controller.SetBool(IsShowing, true);

        // set the character sprite
        SetCharacterIcon(GetSprites(pet).Get(reactionType));
        // bounce
        Controller.SetTrigger(CharacterChanged);

        // queue the dog's name and text
        NameLabel.text = dog.DisplayName;

        // start the typewriter
        Typewriter.PlayTypewriter(dog.GetLine(reactionType, affinity), TypewriterSpeed, delay: TypewriterDelay);
        
        // show the appropriate buttons
        // note: will not be visible yet
        // invite and awoo are mutually exclusive by-design
        AwooButton.SetActiveFast(dog.CanAwoo);
        InviteButton.SetActiveFast(!dog.CanAwoo);
        
        // store for events
        prevPet = pet;
    }

    private CharacterDialogueSpriteCollection GetSprites(PetId id)
    {
        if (CharacterSprites.TryGet((int)id, out var collection))
        {
            if (collection.Character != id)
            {
                Debug.LogWarning("Character does not match id");
            }
            return collection;
        }
        else
        {
            return CharacterSprites.Length > 0 ? CharacterSprites[0] : default;
        }
    }

    private void SetCharacterIcon(GameObject prefab)
    {
        // destroy old if it exists
        foreach (Transform child in CharacterIcon.transform)
        {
            Destroy(child);
        }
        // scale to UI component
        GameObject go = Instantiate(prefab);
        PetDog petdog = go.GetComponent<PetDog>();
        petdog.ScaleToUI(CharacterIcon.gameObject);
    }

    private void Update()
    {
        if (ModeManager.Instance.Mode != GameMode.Dialogue) return;
        
        bool typewriterDone = Typewriter.GetProgressPercent() >= 1;
        
        // handle interaction
        if (Controller.GetBool(IsShowing))
        {
            if (InputHandler.Instance.menu.pressed)
            {
                Debug.Log("esc press");
                OnChoice(PlayerActionType.Leave);
                return;
            }

            // interact is contextual
            if (InputHandler.Instance.interact.pressed && !inputFlag)
            {
                // show options
                if (typewriterDone)
                {
                    Controller.SetBool( OptionsShowing, true);
                }
                // skip
                else
                {
                    typewriterDone = true;
                    Typewriter.Finish();
                }

                inputFlag = true;
            }

            if (InputHandler.Instance.interact.released)
            {
                inputFlag = false;
            }
        }

        bool shouldShowNextArrow = !Controller.GetBool(OptionsShowing) && typewriterDone;
        if (NextArrow.activeSelf != shouldShowNextArrow)
        {
            NextArrow.SetActiveFast(shouldShowNextArrow);
            NextArrow.GetComponent<Animation>()?.Play();
        }
    }

    public void OnChoice(PlayerActionType actionType)
    {
        Controller.SetBool(OptionsShowing, false);
        switch (actionType)
        {
            case PlayerActionType.Leave:
                affinityBarScript.HideAffinity();
                Controller.SetBool(OptionsShowing, false);
                break;
            case PlayerActionType.Awoo:
                Debug.Log("Awoo " + prevPet);
                ModeManager.Instance.ChangeMode(GameMode.AwooDice);
                break;
            case PlayerActionType.Invite:
                Debug.Log("Invited " + prevPet);
                ModeManager.Instance.ChangeMode(GameMode.CupDice);
                break;
        }
        OnHide();
    }

    public void OnAwooPressed()
    {
        OnChoice(PlayerActionType.Awoo);
    }

    public void OnInvitePressed()
    {
        OnChoice(PlayerActionType.Invite);
    }
    
    public void OnLeavePressed()
    {
        OnChoice(PlayerActionType.Leave);
    }

    private void OnHide()
    {
        Controller.SetBool(IsShowing, false);
        
        if (ModeManager.Instance.Mode == GameMode.Dialogue)
        {
            ModeManager.Instance.ChangeMode(GameMode.Bar);
        }
    }
}
