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

    [Header("물약 이동 연출")]
    [SerializeField] private RectTransform potionProjectile;
    [SerializeField] private RectTransform potionStartPoint;
    [SerializeField] private RectTransform playerTarget;
    [SerializeField] private RectTransform enemyTarget;
    [SerializeField] private float potionFlightDuration = 0.45f;
    [SerializeField] private float potionArcHeight = 100f;

    private bool isPotionFlying = false;

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

    [Header("피격 연출")]
    [SerializeField] private HitReactionUI playerHitReaction;
    [SerializeField] private HitReactionUI enemyHitReaction;

    [Header("적 공격 연출")]
    [SerializeField] private AttackMotionUI enemyAttackMotion;

    [Header("체력 바")]
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private Slider enemyHPBar;
    [SerializeField] private GameObject playerArmorFrame;

    [Header("날아가는 물약 이미지")]
    [SerializeField] private Sprite redPotionSprite;
    [SerializeField] private Sprite tealPotionSprite;
    [SerializeField] private Sprite yellowPotionSprite;
    [SerializeField] private Sprite bluePotionSprite;

    [Header("이미지 조합 목록")]
    [SerializeField] private ScrollRect recipeScrollView;
    [SerializeField] private RectTransform recipeOptionsContent;
    [SerializeField] private RecipeOptionUI recipeOptionPrefab;

    // 주머니 원본과 현재 턴의 재료·물약 데이터
    // 테스트 구성: 기본 재료 9개 + 졸음가루 1개.
    private string[] bagTokens =
    {
        "홍등열매", "홍등열매", "홍등열매",
        "갑각잎", "갑각잎",
        "새살꽃", "새살꽃",
        "시듦버섯",
        "바람씨앗",
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
    private bool isPaused = false;

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
        GameSaveData data = GameSession.CurrentSave;

        preparedPotion = null;
        preparedPotionButton.SetActive(false);
        bagPanel.SetActive(false);

        if (potionProjectile != null)
        {
            potionProjectile.gameObject.SetActive(false);
        }

        if (data != null)
        {
            playerMaxHP = data.playerMaxHP;
            playerHP = data.playerHP;

            if (data.bagTokens != null)
            {
                bagTokens = (string[])data.bagTokens.Clone();
            }

            // 전투 중 저장했다면 새 턴을 시작하지 않고 그대로 복원한다.
            if (data.hasBattleState)
            {
                RestoreBattleState(data);
                return;
            }
        }

        enemyHP = enemyMaxHP;

        UpdateEnemyHPUI();
        UpdatePlayerStatusUI();

        StartTurn();
    }

    // 손패를 다시 뽑거나 예약 효과를 발동하지 않고 저장한 상태만 복원한다.
    private void RestoreBattleState(GameSaveData data)
    {
        turnNumber = data.turnNumber;
        hasDrawn = data.hasDrawn;

        playerArmor = data.playerArmor;

        enemyHP = data.enemyHP;
        enemyMaxHP = data.enemyMaxHP;
        enemyAttackDamage = data.enemyAttackDamage;
        enemyWeakTurns = data.enemyWeakTurns;

        remainingTokens = new List<string>(
            data.remainingTokens ?? new string[0]);

        handTokens = new List<string>(
            data.handTokens ?? new string[0]);

        cauldronTokens = new List<string>(
            data.cauldronTokens ?? new string[0]);

        delayedPotions = new List<PotionData>(
            data.delayedPotions ?? new PotionData[0]);

        UpdateEnemyHPUI();
        UpdatePlayerStatusUI();
        RefreshHandUI();
    }

    // 일시정지한 전투를 현재 선택한 슬롯에 저장한다.
    public bool SaveBattle(out string message)
    {
        if (!isPaused
            || isEnemyTurn
            || isPotionFlying
            || preparedPotion != null
            || playerHP <= 0
            || enemyHP <= 0)
        {
            message = "현재는 저장할 수 없습니다.";
            return false;
        }

        int slotNumber = GameSession.SlotNumber;

        if (slotNumber < 1 || slotNumber > 3)
        {
            message = "타이틀에서 저장 슬롯을 선택한 후 시작해주세요.";
            return false;
        }

        GameSaveData data = new GameSaveData();

        data.sceneName = gameObject.scene.name;
        data.hasBattleState = true;

        data.playerHP = playerHP;
        data.playerMaxHP = playerMaxHP;
        data.playerArmor = playerArmor;

        data.turnNumber = turnNumber;
        data.hasDrawn = hasDrawn;

        data.enemyHP = enemyHP;
        data.enemyMaxHP = enemyMaxHP;
        data.enemyAttackDamage = enemyAttackDamage;
        data.enemyWeakTurns = enemyWeakTurns;

        data.bagTokens = (string[])bagTokens.Clone();
        data.remainingTokens = remainingTokens.ToArray();
        data.handTokens = handTokens.ToArray();
        data.cauldronTokens = cauldronTokens.ToArray();
        data.delayedPotions = delayedPotions.ToArray();

        if (GameSession.CurrentSave != null)
        {
            data.battleNumber = GameSession.CurrentSave.battleNumber;
        }

        if (!SaveSystem.Save(slotNumber, data))
        {
            message = "저장에 실패했습니다. Console을 확인해주세요.";
            return false;
        }

        GameSession.SelectSave(slotNumber, data);

        message = "슬롯 " + slotNumber + "에 저장했습니다.";
        return true;
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

    // 공격 전 대기 → 돌진과 피해 적용 → 복귀 → 다음 턴 순서로 진행한다.
    private IEnumerator EnemyTurnRoutine()
    {
        Debug.Log(turnNumber + "턴 종료");
        Debug.Log("적 턴 시작");

        yield return new WaitForSeconds(enemyAttackDelay);

        if (enemyAttackMotion != null)
        {
            yield return enemyAttackMotion.PlayAttack(EnemyTurn);
        }
        else
        {
            EnemyTurn();
        }

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

        if (hpDamage > 0 && playerHitReaction != null)
        {
            playerHitReaction.PlayHit();
        }

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
        return !isPaused
            && !isEnemyTurn
            && !isPotionFlying
            && playerHP > 0
            && enemyHP > 0;
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

    // 물약을 제조한 뒤 대상에게 날려 보낸다.
    public void CraftPotion()
    {
        if (!CanCraftPotion())
        {
            return;
        }

        if (potionProjectile == null
            || potionStartPoint == null
            || playerTarget == null
            || enemyTarget == null)
        {
            Debug.LogError("물약 이동 연출의 연결 항목을 확인해주세요.");
            return;
        }

        // 재료를 비우기 전에 사용할 물약 이미지를 결정한다.
        Sprite selectedSprite = GetPotionSprite(cauldronTokens);
        Image projectileImage = potionProjectile.GetComponent<Image>();

        if (selectedSprite == null || projectileImage == null)
        {
            Debug.LogError(
                "물약 Sprite 연결과 PotionProjectile의 Image를 확인해주세요."
            );
            return;
        }

        projectileImage.overrideSprite = null;
        projectileImage.sprite = selectedSprite;
        projectileImage.color = Color.white;
        projectileImage.preserveAspect = true;

        preparedPotion = PotionRecipes.Create(cauldronTokens);
        cauldronTokens.Clear();

        RectTransform targetPoint =
            preparedPotion.target == PotionTarget.Enemy
                ? enemyTarget
                : playerTarget;

        isPotionFlying = true;
        RefreshHandUI();

        StartCoroutine(FlyPotionRoutine(targetPoint));
    }

    // 물약을 곡선으로 이동시키고, 도착하면 효과를 적용한다.
    private IEnumerator FlyPotionRoutine(RectTransform targetPoint)
    {
        Transform flightParent = potionProjectile.parent;

        Vector3 startPosition = flightParent.InverseTransformPoint(
            potionStartPoint.TransformPoint(potionStartPoint.rect.center)
        );

        Vector3 endPosition = flightParent.InverseTransformPoint(
            targetPoint.TransformPoint(targetPoint.rect.center)
        );

        potionProjectile.localPosition = startPosition;
        potionProjectile.SetAsLastSibling();
        potionProjectile.gameObject.SetActive(true);

        float duration = Mathf.Max(0.05f, potionFlightDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(
                startPosition,
                endPosition,
                progress
            );

            position.y +=
                Mathf.Sin(progress * Mathf.PI) * potionArcHeight;

            potionProjectile.localPosition = position;

            yield return null;
        }

        potionProjectile.localPosition = endPosition;
        potionProjectile.gameObject.SetActive(false);

        // 이동 제한을 해제한 뒤 기존 물약 사용 함수를 호출한다.
        isPotionFlying = false;
        UsePreparedPotion(preparedPotion.target);
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

            if (enemyHitReaction != null)
            {
                enemyHitReaction.PlayHit();
            }

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

    // 전투 상황에 따라 이미지 목록과 상세 설명을 전환한다.
    private void UpdateRecipePreview()
    {
        ClearRecipeOptions();

        recipeText.gameObject.SetActive(true);
        recipeScrollView.gameObject.SetActive(false);

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

        if (isPotionFlying)
        {
            recipeText.text = "물약을 사용하는 중입니다...";
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
            if (ShowSingleIngredientOptions(cauldronTokens[0]))
            {
                return;
            }

            recipeText.text =
                GetSingleIngredientPreview(cauldronTokens[0]);
            return;
        }

        PotionData previewPotion =
            PotionRecipes.Create(cauldronTokens);

        if (previewPotion != null)
        {
            recipeText.text = GetPotionDescription(previewPotion);
            return;
        }

        if (cauldronTokens.Count == 2
            && cauldronTokens.Contains("시듦버섯"))
        {
            if (ShowAdditiveOptions())
            {
                return;
            }

            recipeText.text = GetAdditivePreview();
            return;
        }

        recipeText.text = "현재 제조할 수 없는 조합입니다.";
    }

    // 재료 하나로 만들 수 있는 조합 목록을 보여준다.
    private string GetSingleIngredientPreview(string ingredient)
    {
        string[] baseIngredients =
        {
        "홍등열매",
        "갑각잎",
        "새살꽃",
        "시듦버섯",
        "바람씨앗"
    };

        bool isBaseIngredient =
            System.Array.IndexOf(baseIngredients, ingredient) >= 0;

        if (!isBaseIngredient)
        {
            return "첨가제를 넣었습니다.\n기본 재료 2개를 추가해주세요.";
        }

        StringBuilder description = new StringBuilder();
        description.AppendLine("추가 재료별 조합");
        description.AppendLine();

        foreach (string nextIngredient in baseIngredients)
        {
            List<string> ingredients = new List<string>
        {
            ingredient,
            nextIngredient
        };

            PotionData potion = PotionRecipes.Create(ingredients);

            if (potion != null)
            {
                description.AppendLine(
                    nextIngredient + ": " + potion.potionName
                );
            }
            else if (ingredients.Contains("시듦버섯"))
            {
                description.AppendLine(
                    nextIngredient + ": 첨가제 필요"
                );
            }
        }

        return description.ToString().TrimEnd();
    }

    // 시듦버섯 조합에 추가할 첨가제와 제조 가능 여부를 보여준다.
    private string GetAdditivePreview()
    {
        foreach (string ingredient in cauldronTokens)
        {
            bool isBaseIngredient =
                ingredient == "홍등열매"
                || ingredient == "갑각잎"
                || ingredient == "새살꽃"
                || ingredient == "시듦버섯"
                || ingredient == "바람씨앗";

            if (!isBaseIngredient)
            {
                return "기본 재료를 하나 더 넣어주세요.";
            }
        }

        string[] additives =
        {
        "졸음나방 날개가루",
        "허물달팽이 진액",
        "썩은별 가시",
        "불씨도롱뇽 비늘"
    };

        StringBuilder description = new StringBuilder();
        description.AppendLine("추가할 첨가제");
        description.AppendLine();

        foreach (string additive in additives)
        {
            List<string> ingredients =
                new List<string>(cauldronTokens);

            ingredients.Add(additive);

            PotionData potion = PotionRecipes.Create(ingredients);

            string displayName = additive == "졸음나방 날개가루"
                ? "졸음가루"
                : additive;

            string result = potion != null
                ? potion.potionName
                : "구현 예정";

            description.AppendLine(displayName + ": " + result);
        }

        return description.ToString().TrimEnd();
    }

    private void UpdateEnemyHPUI()
    {
        enemyHPText.text = enemyHP + " / " + enemyMaxHP;

        if (enemyHPBar != null)
        {
            enemyHPBar.minValue = 0;
            enemyHPBar.maxValue = enemyMaxHP;
            enemyHPBar.value = enemyHP;
        }
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

    // 체력 숫자와 방어도 테두리를 함께 갱신한다.
    private void UpdatePlayerStatusUI()
    {
        bool hasArmor = playerArmor > 0;

        playerStatusText.text =
            playerHP + " / " + playerMaxHP;

        if (hasArmor)
        {
            playerStatusText.text += " (" + playerArmor + ")";
        }

        if (playerHPBar != null)
        {
            playerHPBar.minValue = 0;
            playerHPBar.maxValue = playerMaxHP;
            playerHPBar.value = playerHP;
        }

        if (playerArmorFrame != null)
        {
            playerArmorFrame.SetActive(hasArmor);
        }
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

    // 내 턴에 물약 사용이 끝난 상태에서 일시정지할 수 있다.
    public bool CanPauseBattle()
    {
        return CanPlayerAct() && preparedPotion == null;
    }

    public bool PauseBattle()
    {
        if (!CanPauseBattle())
        {
            return false;
        }

        CloseBagPanel();
        isPaused = true;
        RefreshHandUI();

        return true;
    }

    public void ResumeBattle()
    {
        isPaused = false;
        RefreshHandUI();
    }

    // 포함된 재료를 우선순위대로 검사해 물약 외형을 선택한다.
    private Sprite GetPotionSprite(List<string> ingredients)
    {
        if (ingredients.Contains("홍등열매"))
        {
            return redPotionSprite;
        }

        if (ingredients.Contains("갑각잎"))
        {
            return tealPotionSprite;
        }

        if (ingredients.Contains("새살꽃")
            || ingredients.Contains("바람씨앗"))
        {
            return yellowPotionSprite;
        }

        if (ingredients.Contains("시듦버섯")
            || ingredients.Contains("졸음나방 날개가루"))
        {
            return bluePotionSprite;
        }

        return null;
    }

    // 이전 목록을 지우고 새 목록을 준비한다.
    private void ClearRecipeOptions()
    {
        foreach (Transform child in recipeOptionsContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void AddRecipeOption(
        string ingredient,
        string potionName,
        string requirement = "")
    {
        RecipeOptionUI option = Instantiate(
            recipeOptionPrefab,
            recipeOptionsContent
        );

        option.Setup(
            GetTokenSprite(ingredient),
            potionName,
            requirement
        );
    }

    // 목록 크기를 계산한 뒤 맨 왼쪽부터 보여준다.
    private void OpenRecipeOptions()
    {
        recipeText.gameObject.SetActive(false);
        recipeScrollView.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            recipeOptionsContent
        );

        recipeScrollView.StopMovement();
        recipeScrollView.horizontalNormalizedPosition = 0f;
    }

    // 기본 재료 하나와 함께 사용할 수 있는 재료를 표시한다.
    private bool ShowSingleIngredientOptions(string ingredient)
    {
        string[] baseIngredients =
        {
        "홍등열매",
        "갑각잎",
        "새살꽃",
        "시듦버섯",
        "바람씨앗"
    };

        if (System.Array.IndexOf(baseIngredients, ingredient) < 0)
        {
            return false;
        }

        int optionCount = 0;

        foreach (string nextIngredient in baseIngredients)
        {
            List<string> ingredients = new List<string>
        {
            ingredient,
            nextIngredient
        };

            PotionData potion = PotionRecipes.Create(ingredients);
            string requirement = "";

            // 버섯 조합은 졸음가루까지 넣은 결과를 안내한다.
            if (potion == null && ingredients.Contains("시듦버섯"))
            {
                ingredients.Add("졸음나방 날개가루");
                potion = PotionRecipes.Create(ingredients);
                requirement = "졸음가루 필요";
            }

            if (potion == null)
            {
                continue;
            }

            AddRecipeOption(
                nextIngredient,
                potion.potionName,
                requirement
            );

            optionCount++;
        }

        if (optionCount == 0)
        {
            return false;
        }

        OpenRecipeOptions();
        return true;
    }

    // 두 재료에 추가해 실제로 제조할 수 있는 첨가제를 표시한다.
    private bool ShowAdditiveOptions()
    {
        string[] additives =
        {
        "졸음나방 날개가루",
        "허물달팽이 진액",
        "썩은별 가시",
        "불씨도롱뇽 비늘"
    };

        int optionCount = 0;

        foreach (string additive in additives)
        {
            List<string> ingredients =
                new List<string>(cauldronTokens);

            ingredients.Add(additive);

            PotionData potion = PotionRecipes.Create(ingredients);

            if (potion == null)
            {
                continue;
            }

            AddRecipeOption(additive, potion.potionName);
            optionCount++;
        }

        if (optionCount == 0)
        {
            return false;
        }

        OpenRecipeOptions();
        return true;
    }
}
