using TMPro;
using UnityEngine;

namespace PlagueSurvivor
{
    public sealed class DialoguePanel : MonoBehaviour
    {
        readonly DialogueSession session = new DialogueSession();
        PlagueGame game;
        RectTransform root, panel, portraitRect, titleRect, choiceHeading, hintRect, closeRect;
        UnityEngine.UI.Image panelImage, portrait;
        TMP_Text speaker, body, title, hint;
        readonly UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[4];
        readonly TMP_Text[] labels = new TMP_Text[4];
        readonly RectTransform[] optionRects = new RectTransform[4];
        Vector2 lastSize;
        int visibleOptions;
        public bool IsOpen { get { return session.IsOpen; } }
        public int NodeIndex { get { return session.NodeIndex; } }
        public DialogueScript.Node Current { get { return session.Current; } }

        public void Initialize(PlagueGame owner)
        {
            game = owner;
            var canvas = game.status.GetComponentInParent<Canvas>();
            if (!canvas) { Debug.LogError("Dialogue requires the game HUD Canvas."); return; }
            if (!canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()) canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (!UnityEngine.EventSystems.EventSystem.current)
                new GameObject("Dialogue EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            root = Rect("DialogueRoot",canvas.transform,0,0,1,1);
            var dim = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(.005f,.015f,.025f,.78f);
            var header = Rect("HeaderBackdrop",root,0,.89f,1,1).gameObject.AddComponent<UnityEngine.UI.Image>();
            header.color = new Color(.005f,.015f,.025f,1); header.raycastTarget=false;
            var footer = Rect("FooterBackdrop",root,0,0,1,.052f).gameObject.AddComponent<UnityEngine.UI.Image>();
            footer.color = header.color; footer.raycastTarget=false;
            title = Text("TransmissionTitle",root,22,0,0,1,1); titleRect=title.rectTransform;
            title.color=new Color(.52f,.82f,.85f);
            portraitRect=Rect("CharacterPortrait",root,0,0,1,1);
            portrait=portraitRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            portrait.preserveAspect=true;portrait.raycastTarget=false;
            panel=Rect("DialoguePanel",root,0,0,1,1);
            panelImage=panel.gameObject.AddComponent<UnityEngine.UI.Image>(); panelImage.raycastTarget=false;
            // Text stays inside the supplied art's name strip and cyan divider.
            speaker=Text("CharacterName",panel,29,.055f,.665f,.945f,.89f);
            speaker.color=new Color(1,.86f,.65f);
            body=Text("DialogueBody",panel,26,.055f,.14f,.945f,.56f);
            body.alignment=TextAlignmentOptions.TopLeft;
            var heading=Text("ChoiceHeading",root,20,0,0,1,1); heading.text="CHOOSE A RESPONSE";choiceHeading=heading.rectTransform;
            heading.color=new Color(.55f,.79f,.83f);
            for(int i=0;i<4;i++)
            {
                int index=i;
                optionRects[i]=Rect("Option"+(i+1),root,0,0,1,1);
                var background=optionRects[i].gameObject.AddComponent<UnityEngine.UI.Image>();
                buttons[i]=optionRects[i].gameObject.AddComponent<UnityEngine.UI.Button>();
                buttons[i].targetGraphic=background;
                var colors=buttons[i].colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.7f,1,1);colors.pressedColor=new Color(.45f,.7f,.75f);buttons[i].colors=colors;
                var navigation=buttons[i].navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;buttons[i].navigation=navigation;
                buttons[i].onClick.AddListener(() => SelectOption(index));
                labels[i]=Text("OptionText",optionRects[i],24,.10f,.20f,.90f,.80f);
                labels[i].alignment=TextAlignmentOptions.Center;
            }
            hint=Text("Controls",root,18,0,0,1,1);hintRect=hint.rectTransform;hint.alignment=TextAlignmentOptions.Center;
            closeRect=Rect("Close",root,0,0,1,1);
            var closeImage=closeRect.gameObject.AddComponent<UnityEngine.UI.Image>();closeImage.color=new Color(.05f,.18f,.22f,.95f);
            var close=closeRect.gameObject.AddComponent<UnityEngine.UI.Button>();close.targetGraphic=closeImage;close.onClick.AddListener(game.CloseDialogue);
            var closeNavigation=close.navigation;closeNavigation.mode=UnityEngine.UI.Navigation.Mode.None;close.navigation=closeNavigation;
            var closeLabel=Text("Label",closeRect,20,0,0,1,1);closeLabel.text="Close  [ESC]";closeLabel.alignment=TextAlignmentOptions.Center;
            root.gameObject.SetActive(false);
        }

        public bool Begin(DialogueScript script)
        {
            if (!root) return false;
            string error;
            if (!session.Begin(script,out error)) { Debug.LogWarning(error); return false; }
            root.gameObject.SetActive(true);root.SetAsLastSibling();
            var font=script.font ? script.font : game.status.font;
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font=font;
            panelImage.sprite=script.panelImage;panelImage.color=script.panelImage ? Color.white : new Color(.035f,.075f,.09f);
            for(int i=0;i<4;i++) buttons[i].GetComponent<UnityEngine.UI.Image>().sprite=script.optionImage;
            title.text=script.title;
            Refresh();return true;
        }
        public bool SelectOption(int index)
        {
            if (!IsOpen || index < 0 || index >= visibleOptions) return false;
            bool hasChoices=Current.choices!=null && Current.choices.Length>0;
            bool changed=hasChoices ? session.Choose(index) : index==0 && session.Advance();
            if(changed) { if(IsOpen)Refresh();else game.CloseDialogue(); }
            return changed;
        }
        public void HandleInput()
        {
            if(!IsOpen)return;
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)) { game.CloseDialogue();return; }
            KeyCode[] keys={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4};
            for(int i=0;i<keys.Length;i++)if(Input.GetKeyDown(keys[i])){SelectOption(i);return;}
            if((Current.choices==null || Current.choices.Length==0) && (Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))) SelectOption(0);
        }
        public void Close() { session.Close();if(root)root.gameObject.SetActive(false); }
        void Refresh()
        {
            var node=Current;
            speaker.text=node.speaker;body.text=node.text;
            portrait.sprite=node.portrait ? node.portrait : session.Script.defaultPortrait;portrait.enabled=portrait.sprite;
            bool choices=node.choices!=null && node.choices.Length>0;
            visibleOptions=choices ? node.choices.Length : 1;
            choiceHeading.gameObject.SetActive(choices);
            for(int i=0;i<4;i++)
            {
                buttons[i].gameObject.SetActive(i<visibleOptions);
                if(i<visibleOptions)labels[i].text=choices ? (i+1)+"  "+node.choices[i].text : node.nextNode<0 ? "End transmission" : "Continue";
            }
            hint.text=choices ? "Click a response or press 1 - "+visibleOptions+"    |    ESC Close    |    Combat paused" : "Click Continue or press SPACE / ENTER    |    ESC Close    |    Combat paused";
            Canvas.ForceUpdateCanvases();Layout();
        }
        void LateUpdate() { if(IsOpen && root.rect.size!=lastSize)Layout(); }
        void Layout()
        {
            if(!root)return;
            lastSize=root.rect.size;
            float w=lastSize.x,h=lastSize.y;
            float panelWidth=Mathf.Min(w*.92f,h*2.3f),panelHeight=panelWidth/4;
            float bottom=h*.055f,top=bottom+panelHeight;
            Place(panel,(w-panelWidth)/2,bottom,panelWidth,panelHeight);
            float upperBottom=top+h*.035f, upperTop=h*.88f;
            float room=Mathf.Max(10,upperTop-upperBottom);
            float portraitSize=Mathf.Min(w*.31f,room);
            Place(portraitRect,w*.07f+(w*.31f-portraitSize)/2,upperBottom,portraitSize,portraitSize);
            float gap=10, headingHeight=28;
            float optionHeight=Mathf.Min(w*.50f/8,Mathf.Max(12,(room-headingHeight-gap*(visibleOptions-1))/visibleOptions));
            float optionWidth=optionHeight*8;
            float left=w*.44f+(w*.50f-optionWidth)/2;
            for(int i=0;i<visibleOptions;i++)Place(optionRects[i],left,upperBottom+(visibleOptions-1-i)*(optionHeight+gap),optionWidth,optionHeight);
            Place(choiceHeading,left,upperBottom+visibleOptions*optionHeight+(visibleOptions-1)*gap,optionWidth,headingHeight);
            Place(titleRect,w*.055f,h*.91f,w*.65f,h*.06f);
            Place(closeRect,w*.80f,h*.92f,w*.15f,h*.05f);
            Place(hintRect,w*.04f,0,w*.92f,h*.05f);
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);}
        TMP_Text Text(string name,Transform parent,float size,float x0,float y0,float x1,float y1)
        {
            var text=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=game.status.font;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.65f;text.fontSizeMax=size;
            text.color=new Color(.88f,.94f,.95f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;
            return text;
        }
        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        void OnDestroy(){if(root)Destroy(root.gameObject);}
    }
}
