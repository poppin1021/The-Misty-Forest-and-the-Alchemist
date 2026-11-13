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

    [Header("대기 모션")]
    [SerializeField] private IdleBreathingUI playerIdleBreathing;
    [SerializeField] private IdleBreathingUI enemyIdleBreathing;

    [Header("전투 결과")]
    [SerializeField] private BattleResultController battleResultController;

    private bool isBattleOver = false;

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

    internal IReadOnlyList<string> PouchIngredients => bagTokens;

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
    private int enemyVulnerableTurns;
    private int enemyPoisonStacks;
    private int enemyBurnStacks;
    private bool hasAfterheat;
    private bool isChoosingToken;
    private int pendingRandomDraws;
    private TokenChoiceUI tokenChoiceUI;
    private TMP_Text playerBuffText;
    private CombatFeedbackUI combatFeedback;
    [SerializeField] private string battleAreaName = "안개숲 · 숲 입구";
    private BattleHudUI battleHud;
    private TurnBannerUI turnBanner;
    private bool isTurnAnnouncing;

    // 1. 게임 초기화와 턴 진행
    private void Start()
    {
        GameSaveData data = GameSession.CurrentSave;
        if (data?.bagTokens != null)
        {
            bagTokens = (string[])data.bagTokens.Clone();
        }
        CreateAdditionalCombatUI();

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
        turnNumber = Mathf.Max(1, data.turnNumber);
        hasDrawn = data.hasDrawn;

        playerArmor = data.playerArmor;

        enemyHP = data.enemyHP;
        enemyMaxHP = data.enemyMaxHP;
        enemyAttackDamage = data.enemyAttackDamage;
        enemyWeakTurns = Mathf.Max(0, data.enemyWeakTurns);
        enemyVulnerableTurns = Mathf.Max(0, data.enemyVulnerableTurns);
        enemyPoisonStacks = Mathf.Max(0, data.enemyPoisonStacks);
        enemyBurnStacks = Mathf.Max(0, data.enemyBurnStacks);
        hasAfterheat = data.hasAfterheat;

        remainingTokens = new List<string>(
            data.remainingTokens ?? new string[0]);

        handTokens = new List<string>(
            data.handTokens ?? new string[0]);

        cauldronTokens = new List<string>(
            data.cauldronTokens ?? new string[0]);

        delayedPotions = new List<PotionData>();
        foreach (PotionData saved in data.delayedPotions ?? new PotionData[0])
        {
            if (saved == null) continue;
            if (saved.repeatNextTurn)
            {
                PotionData migrated = PotionEffects.CreateRepeat(saved);
                if (saved.drawCount > 0 && saved.delayedDrawCount == 0)
                    migrated.drawCount = 1;
                delayedPotions.Add(migrated);
            }
            else delayedPotions.Add(saved.Copy());
        }

        UpdateEnemyHPUI();
        UpdatePlayerStatusUI();
        isTurnAnnouncing = true;
        if (turnBanner != null) turnBanner.SetCurrentTurn(turnNumber, false);
        RefreshHandUI();
        StartCoroutine(PlayerTurnStartRoutine(true));
    }

    // 일시정지한 전투를 현재 선택한 슬롯에 저장한다.
    public bool SaveBattle(out string message)
    {
        if (!isPaused
            || isEnemyTurn
            || isPotionFlying
            || isChoosingToken
            || isTurnAnnouncing
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
        data.enemyVulnerableTurns = enemyVulnerableTurns;
        data.enemyPoisonStacks = enemyPoisonStacks;
        data.enemyBurnStacks = enemyBurnStacks;
        data.hasAfterheat = hasAfterheat;

        data.bagTokens = (string[])bagTokens.Clone();
        data.remainingTokens = remainingTokens.ToArray();
        data.handTokens = handTokens.ToArray();
        data.cauldronTokens = cauldronTokens.ToArray();
        data.delayedPotions = delayedPotions.ToArray();

        if (GameSession.CurrentSave != null)
        {
            data.battleNumber = GameSession.CurrentSave.battleNumber;
            data.hasSeenPrologue = GameSession.CurrentSave.hasSeenPrologue;
            data.coins = GameSession.CurrentSave.coins;
            data.preparationShop = GameSession.CurrentSave.preparationShop;
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

    // 턴 안내 후 방어도 초기화 → 기본 5개 뽑기 → 예약 효과 순서로 진행한다.
    private void StartTurn()
    {
        if (isBattleOver || isTurnAnnouncing) return;
        isTurnAnnouncing = true;
        if (turnBanner != null) turnBanner.SetCurrentTurn(turnNumber, false);
        CloseBagPanel();
        RefreshHandUI();
        StartCoroutine(PlayerTurnStartRoutine(false));
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

        if (hasAfterheat) ShowCombatFeedback(playerTarget, "잔열 소멸", CombatFeedbackKind.Heat);
        hasAfterheat = false;
        UpdatePlayerStatusUI();
        isEnemyTurn = true;
        RefreshHandUI();
        StartCoroutine(EnemyTurnRoutine());
    }

    // 공격 전 대기 → 돌진과 피해 적용 → 복귀 → 다음 턴 순서로 진행한다.
    private IEnumerator EnemyTurnRoutine()
    {
        float announcementStarted = Time.time;
        isTurnAnnouncing = true;
        if (battleHud != null) battleHud.ClearProgress();
        if (turnBanner != null)
            yield return turnBanner.Play(turnNumber, true);
        isTurnAnnouncing = false;
        if (CheckBattleEnd()) yield break;

        // 기존 공격 전 대기 시간에 안내 시간을 포함해 불필요한 기다림을 줄인다.
        float remainingDelay = Mathf.Max(0f, enemyAttackDelay - (Time.time - announcementStarted));
        if (remainingDelay > 0f) yield return new WaitForSeconds(remainingDelay);
        ApplyPoisonBeforeEnemyAction();
        if (enemyHP > 0)
        {
            if (battleHud != null) battleHud.ShowProgress("적이 공격합니다");
            if (enemyAttackMotion != null)
                yield return enemyAttackMotion.PlayAttack(EnemyTurn);
            else
                EnemyTurn();
        }

        yield return new WaitForSeconds(enemyTurnEndDelay);
        enemyVulnerableTurns = Mathf.Max(0, enemyVulnerableTurns - 1);
        enemyBurnStacks = Mathf.Max(0, enemyBurnStacks - 1);
        isEnemyTurn = false;
        UpdateEnemyStatusUI();
        if (CheckBattleEnd()) yield break;
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
            ShowCombatFeedback(enemyTarget, "약화 적용", CombatFeedbackKind.Debuff);
        }

        int blockedDamage = Mathf.Min(playerArmor, attackDamage);
        int hpDamage = attackDamage - blockedDamage;
        playerArmor -= blockedDamage;
        playerHP = Mathf.Max(0, playerHP - hpDamage);

        if (blockedDamage > 0)
            ShowCombatFeedback(playerTarget, blockedDamage + " 방어"
                + (hpDamage == 0 ? " · 피해 차단" : ""), CombatFeedbackKind.Armor);
        if (hpDamage > 0)
        {
            ShowCombatFeedback(playerTarget, "-" + hpDamage, CombatFeedbackKind.Damage);
            if (playerHitReaction != null) playerHitReaction.PlayHit();
            if (combatFeedback != null) combatFeedback.FlashPlayerDamage();
        }
        if (combatFeedback != null)
            combatFeedback.Pulse(playerTarget, hpDamage > 0 ? CombatFeedbackKind.Damage : CombatFeedbackKind.Armor);

        UpdatePlayerStatusUI();
        UpdateEnemyStatusUI();
        Debug.Log("적 공격! 방어한 피해: " + blockedDamage + ", 받은 피해: " + hpDamage);
    }

    private bool CanPlayerAct()
    {
        return !isBattleOver && !isPaused && !isEnemyTurn
            && !isPotionFlying && !isChoosingToken && !isTurnAnnouncing
            && (battleHud == null || !battleHud.IsPanelOpen)
            && playerHP > 0 && enemyHP > 0;
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
        if (!CanPlayerAct() || preparedPotion != null) return false;
        PotionData potion = PotionRecipes.Create(cauldronTokens);
        return potion != null && playerHP > potion.healthCost;
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

        if (battleHud != null) battleHud.ShowProgress(preparedPotion.potionName + " 제조");
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
        if (!CanPlayerAct() || preparedPotion == null || preparedPotion.target != target) return;
        PotionData potion = PotionEffects.WithAfterheat(preparedPotion, hasAfterheat);
        if (playerHP <= potion.healthCost) return;

        if (hasAfterheat)
            ShowCombatFeedback(playerTarget, "잔열 강화", CombatFeedbackKind.Heat);
        hasAfterheat = false;
        ApplyPotionEffects(potion);
        if (potion.repeatNextTurn)
        {
            delayedPotions.Add(PotionEffects.CreateRepeat(potion));
            ShowCombatFeedback(playerTarget, "다음 턴 재발동 예약", CombatFeedbackKind.Repeat);
        }

        preparedPotion = null;
        preparedPotionButton.SetActive(false);
        RefreshHandUI();
        CheckBattleEnd();
    }

    // 물약 데이터의 효과를 적용한다. 즉시 사용과 다음 턴 재발동이 공유한다.
    private void ApplyPotionEffects(PotionData potion)
    {
        if (potion == null) return;
        if (potion.healthCost > 0)
        {
            int cost = Mathf.Min(potion.healthCost, Mathf.Max(0, playerHP - 1));
            playerHP -= cost;
            ShowCombatFeedback(playerTarget, "-" + cost + " 체력 소모", CombatFeedbackKind.HealthCost);
        }

        if (potion.damage > 0)
        {
            PotionDamage hit = PotionEffects.CalculateDirectDamage(potion.damage, enemyVulnerableTurns, enemyBurnStacks);
            enemyHP = Mathf.Max(0, enemyHP - hit.Total);
            ShowCombatFeedback(enemyTarget, "-" + hit.direct, CombatFeedbackKind.Damage);
            if (hit.burn > 0)
                ShowCombatFeedback(enemyTarget, "-" + hit.burn + " 화상", CombatFeedbackKind.Burn);
            if (combatFeedback != null) combatFeedback.Pulse(enemyTarget, CombatFeedbackKind.Damage);
            if (enemyHitReaction != null) enemyHitReaction.PlayHit();
            Debug.Log("물약 피해 " + hit.direct + " / 화상 추가 피해 " + hit.burn);
        }

        playerArmor += potion.block;
        if (potion.block > 0)
        {
            ShowCombatFeedback(playerTarget, "+" + potion.block + " 방어도", CombatFeedbackKind.Armor);
            if (combatFeedback != null) combatFeedback.Pulse(playerTarget, CombatFeedbackKind.Armor);
        }

        int healed = Mathf.Min(Mathf.Max(0, playerMaxHP - playerHP), potion.healing);
        playerHP = Mathf.Min(playerMaxHP, playerHP + potion.healing);
        if (potion.healing > 0)
        {
            ShowCombatFeedback(playerTarget, healed > 0 ? "+" + healed + " 회복" : "최대 체력", CombatFeedbackKind.Healing);
            if (combatFeedback != null) combatFeedback.Pulse(playerTarget, CombatFeedbackKind.Healing);
        }

        // 직접 피해 이후에 새 상태 이상을 부여하는 기존 순서를 유지한다.
        enemyWeakTurns += potion.weakTurns;
        enemyVulnerableTurns += potion.vulnerableTurns;
        enemyPoisonStacks += potion.poisonStacks;
        enemyBurnStacks += potion.burnStacks;
        if (potion.weakTurns > 0) ShowCombatFeedback(enemyTarget, "약화 +" + potion.weakTurns, CombatFeedbackKind.Debuff);
        if (potion.vulnerableTurns > 0) ShowCombatFeedback(enemyTarget, "취약 +" + potion.vulnerableTurns, CombatFeedbackKind.Debuff);
        if (potion.poisonStacks > 0) ShowCombatFeedback(enemyTarget, "중독 +" + potion.poisonStacks, CombatFeedbackKind.Poison);
        if (potion.burnStacks > 0) ShowCombatFeedback(enemyTarget, "화상 +" + potion.burnStacks, CombatFeedbackKind.Burn);
        if (potion.damage == 0 && combatFeedback != null)
        {
            if (potion.poisonStacks > 0) combatFeedback.Pulse(enemyTarget, CombatFeedbackKind.Poison);
            else if (potion.burnStacks > 0) combatFeedback.Pulse(enemyTarget, CombatFeedbackKind.Burn);
            else if (potion.weakTurns > 0 || potion.vulnerableTurns > 0) combatFeedback.Pulse(enemyTarget, CombatFeedbackKind.Debuff);
        }

        if (potion.grantsAfterheat)
        {
            hasAfterheat = true;
            ShowCombatFeedback(playerTarget, "잔열 획득", CombatFeedbackKind.Heat);
        }

        if (potion.drawCount > 0 && playerHP > 0 && enemyHP > 0)
        {
            if (potion.chooseOneDraw) BeginChoiceDraw(potion.drawCount);
            else DrawAdditionalTokensWithFeedback(potion.drawCount);
        }
        UpdateEnemyHPUI();
        UpdateEnemyStatusUI();
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
            ShowCombatFeedback(playerTarget, "잔향 재발동", CombatFeedbackKind.Repeat);

            ApplyPotionEffects(potion);
        }

        RefreshHandUI();
        CheckBattleEnd();
    }

    // 6. 가방 창과 남은 재료 조회
    public bool CanOpenBattleMenu()
    {
        return CanPauseBattle() && !bagPanel.activeSelf && !SceneTransition.IsTransitioning;
    }

    public void ToggleBagPanel()
    {
        if (isTurnAnnouncing || (battleHud != null && battleHud.IsPanelOpen)) return;
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

        if (isBattleOver || playerHP <= 0 || enemyHP <= 0)
        {
            recipeText.text = "";
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
            if (cauldronTokens.Count == 2)
                recipeText.text += "\n첨가제 1개를 추가할 수 있습니다.";
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

        if (cauldronTokens.Count == 2)
        {
            foreach (string ingredient in cauldronTokens)
            {
                if (!IngredientCatalog.IsAdditive(ingredient)) continue;
                recipeText.text = "기본 재료를 하나 더 넣어주세요.\n"
                    + IngredientCatalog.AdditiveDescription(ingredient);
                return;
            }
        }
        recipeText.text = "기본 재료 2개와 첨가제 최대 1개로 제조합니다.\n시듦버섯 조합에는 첨가제가 필요합니다.";
    }

    // 재료 하나로 만들 수 있는 조합 목록을 보여준다.
    private string GetSingleIngredientPreview(string ingredient)
    {
        if (IngredientCatalog.IsAdditive(ingredient))
            return IngredientCatalog.DisplayName(ingredient) + "\n기본 재료 2개를 추가해주세요.\n"
                + IngredientCatalog.AdditiveDescription(ingredient);
        return "기본 재료 2개와 첨가제 최대 1개로 제조합니다.";
    }

    // 시듦버섯 조합에 추가할 첨가제와 제조 가능 여부를 보여준다.
    private string GetAdditivePreview()
    {
        if (cauldronTokens.Count != 2
            || !IngredientCatalog.IsBase(cauldronTokens[0])
            || !IngredientCatalog.IsBase(cauldronTokens[1]))
            return "기본 재료 2개와 첨가제 최대 1개를 넣어주세요.";

        StringBuilder description = new StringBuilder("추가할 첨가제\n");
        foreach (string additive in IngredientCatalog.Additives)
        {
            List<string> ingredients = new List<string>(cauldronTokens) { additive };
            PotionData potion = PotionRecipes.Create(ingredients);
            if (potion != null)
                description.AppendLine(IngredientCatalog.DisplayName(additive) + ": " + potion.potionName);
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

        int nextAttack = enemyWeakTurns > 0
            ? Mathf.FloorToInt(enemyAttackDamage * 0.75f) : enemyAttackDamage;
        enemyIntentText.text = enemyPoisonStacks >= enemyHP
            ? "중독으로 쓰러질 예정" : "ATK " + nextAttack;

        List<string> statuses = new List<string>();
        if (enemyWeakTurns > 0) statuses.Add("약화 " + enemyWeakTurns);
        if (enemyVulnerableTurns > 0) statuses.Add("취약 " + enemyVulnerableTurns);
        if (enemyPoisonStacks > 0) statuses.Add("중독 " + enemyPoisonStacks);
        if (enemyBurnStacks > 0) statuses.Add("화상 " + enemyBurnStacks);
        enemyDebuffText.text = string.Join("\n", statuses);
    }

    // 체력 숫자와 방어도 테두리를 함께 갱신한다.
    private void UpdatePlayerStatusUI()
    {
        if (battleHud != null)
            battleHud.SetPlayerStatus(playerHP, playerMaxHP, playerArmor, hasAfterheat);
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
        if (playerBuffText != null)
        {
            playerBuffText.text = hasAfterheat ? "잔열: 다음 물약 1개 강화" : "";
        }
    }

    // 8. 표시용 물약 설명과 재료 색상
    private string GetPotionDescription(PotionData potion)
    {
        PotionData effective = PotionEffects.WithAfterheat(potion, hasAfterheat);
        string description = PotionEffects.Describe(effective);
        if (effective == null) return description;

        if (hasAfterheat) description += "\n<color=#FFD480>잔열 강화 적용</color>";
        if (effective.healthCost > 0 && playerHP <= effective.healthCost)
            description += "\n<color=#FF9999>체력이 부족합니다. HP " + (effective.healthCost + 1) + " 이상 필요</color>";
        if (effective.damage > 0 && (enemyVulnerableTurns > 0 || enemyBurnStacks > 0))
        {
            PotionDamage hit = PotionEffects.CalculateDirectDamage(
                effective.damage, enemyVulnerableTurns, enemyBurnStacks
            );
            description += "\n현재 적 예상 피해 " + hit.Total + " (화상 " + hit.burn + " 포함)";
        }
        return description;
    }

    private Color GetTokenColor(string tokenName)
    {
        tokenName = IngredientCatalog.Normalize(tokenName);
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
    internal Sprite GetTokenSprite(string tokenName)
    {
        tokenName = IngredientCatalog.Normalize(tokenName);
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
        if (combatFeedback != null) combatFeedback.SetVisible(false);
        RefreshHandUI();

        return true;
    }

    public void ResumeBattle()
    {
        isPaused = false;
        if (combatFeedback != null) combatFeedback.SetVisible(true);
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
        if (!IngredientCatalog.IsBase(ingredient)) return false;
        foreach (string nextIngredient in IngredientCatalog.BaseIngredients)
        {
            List<string> pair = new List<string> { ingredient, nextIngredient };
            bool requiresAdditive = IngredientCatalog.Normalize(ingredient) == IngredientCatalog.Mushroom
                || nextIngredient == IngredientCatalog.Mushroom;
            AddRecipeOption(nextIngredient, PotionRecipes.GetBaseName(pair),
                requiresAdditive ? "첨가제 필요" : "");
        }
        OpenRecipeOptions();
        return true;
    }

    // 두 재료에 추가해 실제로 제조할 수 있는 첨가제를 표시한다.
    private bool ShowAdditiveOptions()
    {
        if (cauldronTokens.Count != 2
            || !IngredientCatalog.IsBase(cauldronTokens[0])
            || !IngredientCatalog.IsBase(cauldronTokens[1])) return false;

        int count = 0;
        foreach (string additive in IngredientCatalog.Additives)
        {
            List<string> ingredients = new List<string>(cauldronTokens) { additive };
            PotionData potion = PotionRecipes.Create(ingredients);
            if (potion == null) continue;
            string requirement = potion.weakTurns > 0 ? "약화 " + potion.weakTurns
                : potion.vulnerableTurns > 0 ? "취약 " + potion.vulnerableTurns
                : potion.poisonStacks > 0 ? "중독 " + potion.poisonStacks
                : potion.burnStacks > 0 ? "화상 " + potion.burnStacks : "";
            AddRecipeOption(additive, potion.potionName, requirement);
            count++;
        }
        if (count == 0) return false;
        OpenRecipeOptions();
        return true;
    }

    // 전투 종료를 한 번만 처리하고 결과 화면을 요청한다.
    private bool CheckBattleEnd()
    {
        if (isBattleOver)
        {
            return true;
        }

        if (playerHP > 0 && enemyHP > 0)
        {
            return false;
        }

        isBattleOver = true;
        if (battleHud != null)
        {
            battleHud.ClosePanel();
            battleHud.ClearProgress();
        }
        isTurnAnnouncing = false;
        if (turnBanner != null) turnBanner.SetBattleEnded();
        isChoosingToken = false;
        pendingRandomDraws = 0;
        hasAfterheat = false;
        if (tokenChoiceUI != null) tokenChoiceUI.Hide();
        UpdatePlayerStatusUI();

        // 결과 화면에서는 캐릭터와 몬스터의 대기 모션을 멈춘다.
        if (playerIdleBreathing != null)
        {
            playerIdleBreathing.StopIdle();
        }

        if (enemyIdleBreathing != null)
        {
            enemyIdleBreathing.StopIdle();
        }

        CloseBagPanel();
        RefreshHandUI();

        if (battleResultController != null)
        {
            bool isVictory = enemyHP <= 0 && playerHP > 0;

            battleResultController.ShowResult(isVictory);
        }

        return true;
    }

    // 선택한 재료를 유지하고 체력을 회복한 첫 턴 데이터를 만든다.
    public GameSaveData CreateRetryData()
    {
        GameSaveData data = new GameSaveData();

        data.sceneName = gameObject.scene.name;
        data.hasBattleState = false;

        data.playerMaxHP = playerMaxHP;
        data.playerHP = playerMaxHP;
        data.bagTokens = (string[])bagTokens.Clone();

        if (GameSession.CurrentSave != null)
        {
            data.battleNumber = GameSession.CurrentSave.battleNumber;
            data.hasSeenPrologue = GameSession.CurrentSave.hasSeenPrologue;
            data.coins = GameSession.CurrentSave.coins;
            data.preparationShop = GameSession.CurrentSave.preparationShop;
        }
        else
        {
            data.hasSeenPrologue = true;
        }

        return data;
    }


    private void ApplyPoisonBeforeEnemyAction()
    {
        int damage = PotionEffects.CalculatePoisonDamage(enemyPoisonStacks);
        if (damage <= 0) return;
        enemyHP = Mathf.Max(0, enemyHP - damage);
        enemyPoisonStacks = Mathf.Max(0, enemyPoisonStacks - 1);
        ShowCombatFeedback(enemyTarget, "-" + damage + " 중독", CombatFeedbackKind.Poison);
        if (combatFeedback != null) combatFeedback.Pulse(enemyTarget, CombatFeedbackKind.Poison);
        if (enemyHitReaction != null) enemyHitReaction.PlayHit();
        UpdateEnemyHPUI();
        UpdateEnemyStatusUI();
        Debug.Log("중독 피해 " + damage);
    }


    private void BeginChoiceDraw(int drawCount)
    {
        if (remainingTokens.Count == 0)
        {
            DrawAdditionalTokensWithFeedback(drawCount);
            return;
        }

        pendingRandomDraws = Mathf.Max(0, drawCount - 1);
        isChoosingToken = true;
        CloseBagPanel();

        if (tokenChoiceUI.Show(remainingTokens, GetTokenSprite, CompleteChoiceDraw))
        {
            if (battleHud != null) battleHud.ShowProgress("추출할 재료를 선택해주세요");
        }
        else
        {
            isChoosingToken = false;
            pendingRandomDraws = 0;
            DrawAdditionalTokensWithFeedback(drawCount);
        }
    }


    private void CompleteChoiceDraw(string tokenName)
    {
        if (battleHud != null) battleHud.ClearProgress();
        int index = remainingTokens.IndexOf(tokenName);
        int randomDraws = pendingRandomDraws;
        int handCountBefore = handTokens.Count;
        pendingRandomDraws = 0;
        isChoosingToken = false;
        if (index >= 0)
        {
            handTokens.Add(remainingTokens[index]);
            remainingTokens.RemoveAt(index);
        }
        else randomDraws++;
        DrawAdditionalTokens(randomDraws);
        int drawn = handTokens.Count - handCountBefore;
        if (drawn > 0)
            ShowCombatFeedback(playerTarget, (index >= 0 ? "선택 추출 포함 +" : "추출 +") + drawn, CombatFeedbackKind.Draw);
        RefreshHandUI();
        CheckBattleEnd();
    }


    private void CreateAdditionalCombatUI()
    {
        Canvas rootCanvas = handArea.GetComponentInParent<Canvas>().rootCanvas;
        tokenChoiceUI = TokenChoiceUI.Create(rootCanvas.transform, playerStatusText.font);
        combatFeedback = CombatFeedbackUI.Create(rootCanvas.transform, playerStatusText.font);
        Sprite portrait = playerTarget != null ? playerTarget.GetComponent<Image>()?.sprite : null;
        battleHud = BattleHudUI.Create(rootCanvas.transform, playerStatusText.font, portrait,
            this, FindAnyObjectByType<PauseMenuController>(), battleAreaName);
        turnBanner = TurnBannerUI.Create(rootCanvas.transform, playerStatusText.font, battleHud.TurnText);

        playerBuffText = Instantiate(playerStatusText, playerHPBar.transform);
        playerBuffText.name = "PlayerBuffText";
        playerBuffText.fontSize = 22f;
        playerBuffText.enableAutoSizing = false;
        playerBuffText.raycastTarget = false;
        playerBuffText.alignment = TextAlignmentOptions.Center;
        playerBuffText.margin = Vector4.zero;
        playerBuffText.text = "";
        RectTransform buffRect = playerBuffText.rectTransform;
        buffRect.anchorMin = buffRect.anchorMax = new Vector2(0.5f, 0f);
        buffRect.pivot = new Vector2(0.5f, 1f);
        buffRect.anchoredPosition = new Vector2(0f, -6f);
        buffRect.sizeDelta = new Vector2(340f, 32f);
        buffRect.localScale = Vector3.one;

        recipeText.enableAutoSizing = true;
        recipeText.fontSizeMin = 18f;
        recipeText.fontSizeMax = 24f;
        recipeText.raycastTarget = false;
        enemyDebuffText.enableAutoSizing = true;
        enemyDebuffText.fontSizeMin = 20f;
        enemyDebuffText.fontSizeMax = 28f;
        enemyDebuffText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180f);
        enemyIntentText.enableAutoSizing = true;
        enemyIntentText.fontSizeMin = 20f;
        enemyIntentText.fontSizeMax = 30f;
    }


    private void DrawAdditionalTokensWithFeedback(int count)
    {
        int before = handTokens.Count;
        DrawAdditionalTokens(count);
        int drawn = handTokens.Count - before;
        ShowCombatFeedback(playerTarget, drawn > 0 ? "추출 +" + drawn : "추출할 재료 없음", CombatFeedbackKind.Draw);
    }


    private void ShowCombatFeedback(RectTransform target, string text, CombatFeedbackKind kind)
    {
        if (combatFeedback != null) combatFeedback.Show(target, text, kind);
    }


    private IEnumerator PlayerTurnStartRoutine(bool continuing)
    {
        while (SceneTransition.IsTransitioning) yield return null;
        if (CheckBattleEnd())
        {
            isTurnAnnouncing = false;
            yield break;
        }
        if (battleHud != null) battleHud.ClearProgress();
        if (turnBanner != null)
            yield return turnBanner.Play(turnNumber, false, continuing);
        isTurnAnnouncing = false;
        if (CheckBattleEnd()) yield break;

        if (!continuing)
        {
            playerArmor = 0;
            UpdatePlayerStatusUI();
            hasDrawn = false;
            cauldronTokens.Clear();
            Debug.Log(turnNumber + "턴 시작");
            DrawTokens();
            ApplyDelayedPotionEffects();
        }
        RefreshHandUI();
    }

}
