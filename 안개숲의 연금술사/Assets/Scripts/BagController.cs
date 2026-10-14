using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 주머니, 턴 진행, 물약 제조·사용과 전투 UI를 관리한다.
public class BagController : MonoBehaviour
{

    [Header("재료와 제조 UI")]
    [SerializeField] private Transform handArea;
    [SerializeField] private GameObject tokenButtonPrefab;
    [SerializeField] private Transform cauldronArea;
    [SerializeField] private TMP_Text recipeText;
    [SerializeField] private Button craftButton;
    [SerializeField] private GameObject preparedPotionButton;

    [Header("재료 아이콘")]
    [SerializeField] private Sprite redBerrySprite;
    [SerializeField] private Sprite armorLeafSprite;
    [SerializeField] private Sprite healingFlowerSprite;
    [SerializeField] private Sprite wiltedMushroomSprite;
    [SerializeField] private Sprite windSeedSprite;
    [SerializeField] private Sprite sleepPowderSprite;
    [SerializeField] private Sprite snailSlimeSprite;
    [SerializeField] private Sprite rottenStarThornSprite;
    [SerializeField] private Sprite emberScaleSprite;

    [Header("전투 상태 UI")]
    [SerializeField] private TMP_Text enemyHPText;
    [SerializeField] private TMP_Text playerStatusText;
    [SerializeField] private TMP_Text enemyIntentText;
    [SerializeField] private TMP_Text enemyDebuffText;

    [Header("가방 확인 UI")]
    [SerializeField] private GameObject bagPanel;
    [SerializeField] private TMP_Text bagContentsText;

    [Header("적 턴 대기 시간")]
    [SerializeField] private float enemyAttackDelay = 0.8f;
    [SerializeField] private float enemyTurnEndDelay = 0.5f;

    // 주머니 원본과 현재 턴의 재료·물약 데이터
    // 테스트 구성: 기본 재료 9개 + 졸음가루 1개.
    private string[] bagTokens =
    {
        "홍등열매",
        "갑각잎", "갑각잎",
        "새살꽃", "새살꽃",
        "시듦버섯", "시듦버섯",
        "바람씨앗", "바람씨앗",
        "졸음나방 날개가루"
    };

    private List<string> remainingTokens = new List<string>();
    private List<string> handTokens = new List<string>();
    private List<string> cauldronTokens = new List<string>();
    private PotionData preparedPotion;
    private List<PotionData> delayedPotions = new List<PotionData>();

    // 턴 상태
    private int turnNumber = 1;
    private bool hasDrawn = false;
    private bool isEnemyTurn = false;

    // 플레이어와 적의 전투 수치
    private int playerMaxHP = 60;
    // 회복 효과를 확인하기 위해 최대 체력보다 낮게 시작한다.
    private int playerHP = 40;
    private int playerArmor = 0;

    private int enemyMaxHP = 30;
    private int enemyHP;
    private int enemyAttackDamage = 8;
    // 약화의 남은 횟수는 적의 공격마다 하나씩 소모한다.
    private int enemyWeakTurns = 0;

    // 1. 게임 초기화와 턴 진행
    private void Start()
    {
        enemyHP = enemyMaxHP;

        UpdateEnemyHPUI();
        UpdatePlayerStatusUI();

        preparedPotionButton.SetActive(false);
        bagPanel.SetActive(false);

        StartTurn();
    }

    // 방어도 초기화 → 기본 5개 뽑기 → 예약된 물약 재발동 순서로 진행한다.
    private void StartTurn()
    {
        playerArmor = 0;
        UpdatePlayerStatusUI();

        hasDrawn = false;
        cauldronTokens.Clear();

        Debug.Log(turnNumber + "턴 시작");
        DrawTokens();
        ApplyDelayedPotionEffects();
    }

    public void EndTurn()
    {
        if (!CanPlayerAct())
        {
            return;
        }

        if (preparedPotion != null)
        {
            Debug.Log("완성된 물약을 먼저 사용해주세요.");
            return;
        }

        isEnemyTurn = true;
        RefreshHandUI();
        StartCoroutine(EnemyTurnRoutine());
    }

    // 적 공격 전후에 기다린다. 추후 이 구간에 공격 애니메이션을 연결한다.
    private IEnumerator EnemyTurnRoutine()
    {
        Debug.Log(turnNumber + "턴 종료");
        Debug.Log("적 턴 시작");
        yield return new WaitForSeconds(enemyAttackDelay);
        EnemyTurn();
        yield return new WaitForSeconds(enemyTurnEndDelay);
        isEnemyTurn = false;

        if (playerHP <= 0)
        {
            RefreshHandUI();
            yield break;
        }

        turnNumber++;
        StartTurn();
    }

    // 약화를 반영한 피해를 방어도로 먼저 막고, 남은 피해를 체력에 적용한다.
    private void EnemyTurn()
    {
        int attackDamage = enemyAttackDamage;

        if (enemyWeakTurns > 0)
        {
            attackDamage = Mathf.FloorToInt(attackDamage * 0.75f);

            enemyWeakTurns--;

            Debug.Log("약화 적용! 이번 공격력: " + attackDamage);
        }

        int blockedDamage = Mathf.Min(playerArmor, attackDamage);

        int hpDamage = attackDamage - blockedDamage;

        playerArmor -= blockedDamage;

        playerHP = Mathf.Max(0, playerHP - hpDamage);

        UpdatePlayerStatusUI();
        UpdateEnemyStatusUI();

        Debug.Log(
            "적 공격! 방어한 피해: " + blockedDamage
            + ", 받은 피해: " + hpDamage
        );

        if (playerHP == 0)
        {
            Debug.Log("플레이어 쓰러짐! 전투 패배!");
        }
    }

    private bool CanPlayerAct()
    {
        return !isEnemyTurn && playerHP > 0 && enemyHP > 0;
    }

    // 2. 재료 뽑기
    // 턴 시작에만 주머니를 전체 구성으로 복원한다.
    public void DrawTokens()
    {
        if (hasDrawn || !CanPlayerAct())
        {
            return;
        }

        hasDrawn = true;
        handTokens.Clear();
        remainingTokens = new List<string>(bagTokens);
        DrawAdditionalTokens(5);
        Debug.Log("꺼낸 재료: " + string.Join(", ", handTokens));
        RefreshHandUI();
    }

    // 추가 뽑기는 남은 주머니만 사용하며, 중간에 주머니를 리필하지 않는다.
    private void DrawAdditionalTokens(int drawCount)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (remainingTokens.Count == 0)
            {
                Debug.Log("주머니가 비어서 더 뽑을 수 없습니다.");
                break;
            }

            int randomIndex = Random.Range(0, remainingTokens.Count);
            string drawnToken = remainingTokens[randomIndex];
            remainingTokens.RemoveAt(randomIndex);
            handTokens.Add(drawnToken);
            Debug.Log("토큰 추출: " + drawnToken);
        }

        Debug.Log("남은 토큰 개수: " + remainingTokens.Count);
    }

    // 3. 손과 가마솥 사이의 재료 이동
    private void MoveToCauldron(int tokenIndex)
    {
        if (!CanPlayerAct())
        {
            return;
        }

        if (preparedPotion != null)
        {
            Debug.Log("완성된 물약을 먼저 사용해주세요.");
            return;
        }

        if (cauldronTokens.Count >= 3)
        {
            Debug.Log("가마솥에는 재료를 최대 3개까지 넣을 수 있습니다.");
            return;
        }

        string token = handTokens[tokenIndex];
        handTokens.RemoveAt(tokenIndex);
        cauldronTokens.Add(token);
        RefreshHandUI();
    }

    private void ReturnToHand(int tokenIndex)
    {
        if (!CanPlayerAct())
        {
            return;
        }

        string token = cauldronTokens[tokenIndex];
        cauldronTokens.RemoveAt(tokenIndex);
        handTokens.Add(token);
        RefreshHandUI();
    }

    public bool CanDragTokens()
    {
        return CanPlayerAct()
            && preparedPotion == null
            && !bagPanel.activeSelf;
    }

    // 드래그 요청도 클릭과 같은 이동 함수를 사용한다.
    public void TryMoveTokenByDrag(int tokenIndex, bool fromCauldron)
    {
        if (!CanDragTokens())
        {
            return;
        }

        if (fromCauldron)
        {
            if (tokenIndex < 0 || tokenIndex >= cauldronTokens.Count)
            {
                return;
            }

            ReturnToHand(tokenIndex);
        }
        else
        {
            if (tokenIndex < 0 || tokenIndex >= handTokens.Count)
            {
                return;
            }

            MoveToCauldron(tokenIndex);
        }
    }

    // 4. 물약 제조와 사용
    private bool CanCraftPotion()
    {
        return CanPlayerAct()
            && preparedPotion == null
            && PotionRecipes.Create(cauldronTokens) != null;
    }

    public void CraftPotion()
    {
        if (!CanCraftPotion())
        {
            return;
        }

        preparedPotion = PotionRecipes.Create(cauldronTokens);
        cauldronTokens.Clear();
        preparedPotionButton.SetActive(true);
        Debug.Log("물약 제조 완료: " + preparedPotion.potionName);
        RefreshHandUI();
    }

    public void UsePreparedPotionOnEnemy()
    {
        UsePreparedPotion(PotionTarget.Enemy);
    }

    public void UsePreparedPotionOnSelf()
    {
        UsePreparedPotion(PotionTarget.Self);
    }

    private void UsePreparedPotion(PotionTarget target)
    {
        if (!CanPlayerAct() || preparedPotion == null)
        {
            return;
        }

        if (preparedPotion.target != target)
        {
            Debug.Log(preparedPotion.target == PotionTarget.Enemy
                ? "이 물약은 적에게 사용해야 합니다."
                : "이 물약은 자신에게 사용해야 합니다.");
            return;
        }

        ApplyPotionEffects(preparedPotion);

        if (preparedPotion.repeatNextTurn)
        {
            delayedPotions.Add(preparedPotion);
        }

        preparedPotion = null;
        preparedPotionButton.SetActive(false);
        RefreshHandUI();
    }

    // 물약 데이터의 효과를 적용한다. 즉시 사용과 다음 턴 재발동이 공유한다.
    private void ApplyPotionEffects(PotionData potion)
    {
        if (potion == null)
        {
            return;
        }

        if (potion.damage > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - potion.damage);
            UpdateEnemyHPUI();
            Debug.Log("적에게 피해 " + potion.damage);

            if (enemyHP == 0)
            {
                Debug.Log("적 처치! 전투 승리!");
            }
        }

        if (potion.block > 0)
        {
            playerArmor += potion.block;
            Debug.Log("방어도 획득 " + potion.block);
        }

        if (potion.healing > 0)
        {
            playerHP = Mathf.Min(playerMaxHP, playerHP + potion.healing);
            Debug.Log("체력 회복 " + potion.healing);
        }

        if (potion.drawCount > 0)
        {
            DrawAdditionalTokens(potion.drawCount);
        }

        if (potion.weakTurns > 0)
        {
            enemyWeakTurns += potion.weakTurns;

            Debug.Log("적에게 약화 부여: " + potion.weakTurns + "회");
        }

        UpdatePlayerStatusUI();
    }

    // 5. 다음 턴 재발동
    private void ApplyDelayedPotionEffects()
    {
        if (delayedPotions.Count == 0)
        {
            return;
        }

        List<PotionData> potionsToApply =
            new List<PotionData>(delayedPotions);

        // 먼저 예약을 비워 같은 물약이 이후 턴에도 계속 발동하지 않게 한다.
        delayedPotions.Clear();

        foreach (PotionData potion in potionsToApply)
        {
            Debug.Log("물약 재발동: " + potion.potionName);

            ApplyPotionEffects(potion);
        }

        RefreshHandUI();
    }

    // 6. 가방 창과 남은 재료 조회
    public void ToggleBagPanel()
    {
        bool shouldOpen = !bagPanel.activeSelf;
        bagPanel.SetActive(shouldOpen);

        if (shouldOpen)
        {
            bagPanel.transform.SetAsLastSibling();
            RefreshBagUI();
        }
    }

    public void CloseBagPanel()
    {
        bagPanel.SetActive(false);
    }

    private void RefreshBagUI()
    {
        if (!bagPanel.activeSelf)
        {
            return;
        }

        Dictionary<string, int> counts = new Dictionary<string, int>();
        List<string> tokenOrder = new List<string>();

        // 0개가 된 종류도 표시하도록 원본 주머니의 종류를 먼저 등록한다.
        foreach (string token in bagTokens)
        {
            if (!counts.ContainsKey(token))
            {
                counts.Add(token, 0);
                tokenOrder.Add(token);
            }
        }

        // 손과 가마솥의 재료를 제외하고, 아직 뽑지 않은 재료만 센다.
        foreach (string token in remainingTokens)
        {
            counts[token]++;
        }

        StringBuilder contents = new StringBuilder();
        contents.AppendLine("미추출 토큰: " + remainingTokens.Count
            + " / 전체 " + bagTokens.Length + "개");
        contents.AppendLine();

        foreach (string token in tokenOrder)
        {
            contents.AppendLine(token + "  × " + counts[token]);
        }

        contents.AppendLine();
        contents.Append("손과 가마솥에 꺼낸 재료는 제외됩니다.");
        bagContentsText.text = contents.ToString();
    }

    // 7. 재료 버튼과 전투 UI 갱신
    private void RefreshHandUI()
    {
        ClearTokenButtons(handArea);
        ClearTokenButtons(cauldronArea);

        for (int i = 0; i < handTokens.Count; i++)
        {
            // 버튼마다 별도 번호를 저장해 클릭 시 해당 재료를 이동한다.
            int tokenIndex = i;
            Button button = CreateTokenButton(handTokens[i], handArea);
            button.GetComponent<TokenDragHandler>().Initialize(
                this, tokenIndex, false, cauldronArea.parent as RectTransform);
            button.onClick.AddListener(() => MoveToCauldron(tokenIndex));
        }

        for (int i = 0; i < cauldronTokens.Count; i++)
        {
            // 버튼마다 별도 번호를 저장해 클릭 시 해당 재료를 이동한다.
            int tokenIndex = i;
            Button button = CreateTokenButton(cauldronTokens[i], cauldronArea);
            button.GetComponent<TokenDragHandler>().Initialize(
                this, tokenIndex, true, handArea as RectTransform);
            button.onClick.AddListener(() => ReturnToHand(tokenIndex));
        }

        UpdateRecipePreview();
        craftButton.interactable = CanCraftPotion();
        RefreshBagUI();
        UpdateEnemyStatusUI();
    }

    private void ClearTokenButtons(Transform area)
    {
        foreach (Transform child in area)
        {
            // Destroy는 프레임 끝에 처리되므로 먼저 숨겨 레이아웃 중복을 막는다.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    // 재료 버튼에 아이콘과 입력 가능 상태를 설정한다.
    private Button CreateTokenButton(string token, Transform parent)
    {
        GameObject newButton = Instantiate(tokenButtonPrefab, parent);

        Image tokenIcon = newButton.transform
            .Find("TokenIcon")
            .GetComponent<Image>();

        Sprite tokenSprite = GetTokenSprite(token);

        tokenIcon.sprite = tokenSprite;
        tokenIcon.color = Color.white;
        tokenIcon.preserveAspect = true;
        tokenIcon.enabled = tokenSprite != null;

        Button button = newButton.GetComponent<Button>();
        button.interactable = CanPlayerAct();

        return button;
    }

    // 전투 결과·적 턴·완성 물약·조합 미리보기 순으로 표시 우선순위를 정한다.
    private void UpdateRecipePreview()
    {
        if (playerHP <= 0)
        {
            recipeText.text = "전투 패배!";
            return;
        }

        if (enemyHP <= 0)
        {
            recipeText.text = "전투 승리!";
            return;
        }

        if (isEnemyTurn)
        {
            recipeText.text = "적이 공격합니다...";
            return;
        }

        if (preparedPotion != null)
        {
            recipeText.text = GetPotionDescription(preparedPotion);
            return;
        }

        if (cauldronTokens.Count == 0)
        {
            recipeText.text = "재료를 가마솥에 넣어주세요.";
            return;
        }

        if (cauldronTokens.Count == 1)
        {
            recipeText.text = "기본 재료를 하나 더 넣어주세요.";
            return;
        }

        PotionData previewPotion = PotionRecipes.Create(cauldronTokens);

        if (previewPotion != null)
        {
            recipeText.text = GetPotionDescription(previewPotion);
            return;
        }

        if (cauldronTokens.Count == 2
            && cauldronTokens.Contains("시듦버섯"))
        {
            recipeText.text = "시듦버섯 조합에는\n첨가제가 필요합니다.";
            return;
        }

        recipeText.text = "현재 제조할 수 없는 조합입니다.";
    }

    private void UpdateEnemyHPUI()
    {
        enemyHPText.text = "HP " + enemyHP + " / " + enemyMaxHP;
    }

    private void UpdateEnemyStatusUI()
    {
        if (enemyHP <= 0 || playerHP <= 0)
        {
            enemyIntentText.text = "";
            enemyDebuffText.text = "";
            return;
        }

        int nextAttackDamage = enemyAttackDamage;

        if (enemyWeakTurns > 0)
        {
            nextAttackDamage =
                Mathf.FloorToInt(nextAttackDamage * 0.75f);
        }

        enemyIntentText.text = "ATK " + nextAttackDamage;

        if (enemyWeakTurns > 0)
        {
            enemyDebuffText.text = "WEAK " + enemyWeakTurns;
        }
        else
        {
            enemyDebuffText.text = "";
        }
    }

    private void UpdatePlayerStatusUI()
    {
        playerStatusText.text = "HP " + playerHP + " / " + playerMaxHP
            + "\nARMOR " + playerArmor;
    }

    // 8. 표시용 물약 설명과 재료 색상
    private string GetPotionDescription(PotionData potion)
    {
        List<string> effects = new List<string>();

        if (potion.damage > 0)
        {
            effects.Add("적에게 피해 " + potion.damage);
        }

        if (potion.block > 0)
        {
            effects.Add("자신에게 방어도 " + potion.block);
        }

        if (potion.healing > 0)
        {
            effects.Add("자신의 체력 회복 " + potion.healing);
        }

        if (potion.drawCount > 0)
        {
            effects.Add("토큰 추가 추출 " + potion.drawCount + "개");
        }

        if (potion.weakTurns > 0)
        {
            effects.Add("적에게 약화 " + potion.weakTurns + "회");
        }

        if (potion.repeatNextTurn)
        {
            effects.Add("다음 내 턴에 효과 1회 재발동");
        }

        string targetText = potion.target == PotionTarget.Enemy
            ? "사용 대상: 적"
            : "사용 대상: 자신";

        return potion.potionName + "\n\n"
            + string.Join("\n", effects) + "\n" + targetText;
    }

    private Color GetTokenColor(string tokenName)
    {
        switch (tokenName)
        {
            case "홍등열매":
                return new Color32(230, 110, 100, 255);
            case "갑각잎":
                return new Color32(100, 190, 170, 255);
            case "새살꽃":
                return new Color32(240, 165, 195, 255);
            case "시듦버섯":
                return new Color32(180, 140, 210, 255);
            case "바람씨앗":
                return new Color32(240, 210, 100, 255);
            case "졸음나방 날개가루":
                return new Color32(130, 190, 235, 255);
            default:
                return Color.white;
        }
    }

    // 조합에 사용하는 재료 이름을 표시용 스프라이트와 연결한다.
    private Sprite GetTokenSprite(string tokenName)
    {
        switch (tokenName)
        {
            case "홍등열매":
                return redBerrySprite;

            case "갑각잎":
                return armorLeafSprite;

            case "새살꽃":
                return healingFlowerSprite;

            case "시듦버섯":
                return wiltedMushroomSprite;

            case "바람씨앗":
                return windSeedSprite;

            case "졸음나방 날개가루":
                return sleepPowderSprite;

            case "허물달팽이 진액":
                return snailSlimeSprite;

            case "썩은별 가시":
                return rottenStarThornSprite;

            case "불씨도롱뇽 비늘":
                return emberScaleSprite;

            default:
                return null;
        }
    }
}
