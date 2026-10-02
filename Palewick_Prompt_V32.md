برومبت Palewick V32 — المرجع الكامل والنهائي للمشروع
آخر تحديث: 1/10/2026
Unity 6000.6.1f1 | Android | Photon PUN 2 | IL2CPP | ARM64 | LZ4

====================================================================
0. قواعد الرد والعمل (إلزامية مطلقة)
====================================================================
• كل رد للمستخدم باللهجة العراقية فقط. ممنوع الرد بالإنكليزي ولا برسالة وحدة.
• كل رسالة خطوة واحدة قصيرة ومرقمة. المستخدم مبتدئ، اشرح ضغطة بضغطة.
• عند تسليم ملف: فقط رابط GitHub + المسار داخل المشروع، لا شيء غيره.
• ممنوع شرح: افتح Notepad، انسخ، الصق، Ctrl+A، Save As، "ارجع لـ Unity وانتظر التحميل". المستخدم يعرفها.
• ممنوع كتابة "شوف الـ Console وأرسل الأخطاء" — المستخدم يخبر بنفسه.
• ممنوع كتابة "لما تخلص كول خلصت" بآخر الخطوات — أعطِ الخطوة التالية مباشرة.
• الكود يُرسل كملف كامل جاهز للنسخ: بدون تعليقات، بدون أسطر فارغة زائدة، بدون Markdown داخل الملف.
• قبل تعديل سكربت موجود اطلب نصه الحالي إذا مو متوفر.
• لا تستخدم ملف أو مسار غير مذكور بالبرومبت قبل ما تطلبه.
• لا تفترض حالة Inspector أو Prefab أو Scene. الصور تُستخدم لفهم Inspector/Scene فقط.
• احفظ كل مسار جديد بالبرومبت أول ما يظهر.
• لا نسخ احتياطية إطلاقاً (لا تقترحها ولا تسويها).
• لا تعمل Build بوجود Error.
• ممنوع حذف Assets/_TerrainAutoUpgrade.
• لا تغيّر Player Settings / Quality / Physics / Audio / Camera / Lighting / Canvas Anchors / Transform الماب بدون طلب صريح.
• لا تغيّر قيم الحركة بدون طلب صريح.
• المحرر المعتمد: VS Code (مربوط بـ Unity عبر Visual Studio Code Editor Package v2.0.27)، والـ Agent يشتغل من متصفح Brave فقط.

====================================================================
1. هوية المشروع والإعدادات الأساسية
====================================================================
اللعبة: Palewick — Horror Multiplayer — Android فقط — Photon PUN 2.
Unity: 6000.6.1f1 | Scripting Backend: IL2CPP | Architecture: ARM64 | Compression: LZ4.
Input System: Old Input Manager فقط (New Input System ممنوع نهائياً).
مسار المشروع: C:\Users\yf_hdr\Desktop\yarekam
ناتج البناء الدائم: C:\Users\yf_hdr\Desktop\apkk\Palewick_v1.0.apk (كل Build جديد يستبدل نفس الملف).
مشروع Unity Cloud: Palewick (المؤسسة نفس الاسم). الحساب: yousif6889.

====================================================================
2. ترتيب المشاهد
====================================================================
1. Assets/a.loby/Scene_Intro
2. Assets/a.loby/Scene_Lobby
3. Assets/a.last/Flooded_Grounds/Scenes/Scene_A

====================================================================
3. سكربتات Runtime ومساراتها (Assets/Scripts إلا المذكور)
====================================================================
AutoRunButton.cs | BloodEffectUI.cs | ChatSystem.cs | DeathScreen.cs | DoorController.cs | EnemyAI.cs
FlashlightSway.cs | FootstepSoundController.cs | GTA6CameraEffects.cs | HeartbeatSfx.cs | IntroManager.cs
JumpButton.cs | LoadingScreenFx.cs | LobbyManager.cs | Minimap.cs | NetworkManager.cs | PauseMenuPUBG.cs
PlayerHealth.cs | PlayerInteraction.cs | PlayerSetup.cs | PointPickup.cs | PwAds.cs | PwAuthUI.cs
PwCloud.cs | PwGoogle.cs | PwLanguageUI.cs | PwLocalizer.cs | PwPoints.cs | PwPointsHud.cs | PwStartMode.cs
PwHorrorSettingsFx.cs | PwPerformance.cs | PwShop.cs | PwShopUI.cs | PwLoadout.cs
ServerBrowser.cs
Assets/a.last/Flooded_Grounds/Scripts/: FlashlightController.cs | StaminaSystem.cs | CameraViewSwitcher.cs | FPSController/CharController_Motor.cs
PwRtl: كلاس static داخل PauseMenuPUBG.cs (PwRtl.Visual لتشكيل العربي/الكردي).
IInteractable: واجهة معرّفة داخل PlayerInteraction.cs — أي Pickup أو باب ينفّذها.

====================================================================
4. أدوات Editor (Assets/Editor) — Namespace إجباري: Palewick.EditorTools
====================================================================
LobbyBuilder.cs ................ Palewick/Build Horror Lobby (يمسح الكانفس ويبني اللوبي + ينادي PwLoginBuilder)
GameScreensBuilder.cs .......... Palewick/Build Horror Game Screens (داخل DeathPanel + LoadingPanel)
IntroBuilder.cs ................ Palewick/Build Horror Intro
ChatBuilder.cs ................. Palewick/Create Chat In Canvas
MinimapBuilder.cs .............. بناء المينيماب
HudCornerLock.cs ............... Palewick/Lock HUD To Screen Corners (يتخطى PointsBadge)
PwLoginBuilder.cs .............. Palewick/Build Login Screen (شاشة اللغة + الدخول + سؤال أونلاين/أوفلاين + شارة النقاط)
PwPointsBuilder.cs ............. Palewick/Build Points HUD + Palewick/Create Point Pickup
PwServicesDefines.cs ........... Palewick/Refresh Service Defines (يضيف PW_ADMOB / PW_GPGS تلقائياً)
PwPluginMetaFixer.cs ........... Palewick/Fix Plugin Meta Files (يرقّي ملفات .meta القديمة للبلَكنات)
BuildWarningsFixer.cs | PreBakeCollisionFixer.cs | HorrorLightingTool.cs | DoorSetupTool.cs | MapCollisionTool.cs
MarkStaticTool.cs | NavMeshBakerTool.cs | PhotonCrashPreventer.cs | ToggleNavMeshTool.cs
MobileOptimizationTool.cs ...... Palewick/Optimize Mobile Performance (Canvas Scaler 1920x1080 + إطفاء raycast للنصوص + ASTC للأندرويد)
MissingMaterialFixer.cs ........ Palewick/Fix Missing Materials (يصلح المواد البيضاء/الشيدرات المكسورة بالمشهد المفتوح)
أي Editor Tool جديد يحتاج طلباً صريحاً.

====================================================================
5. البريفابات، الموديلات، الشيدرات واللاعب
====================================================================
• اللاعب (حقائق ثابتة):
  - اللاعب غير موجود بمشهد Scene_A — Photon يولّده أونلاين من Assets/Resources/WhiteclownPlayer.prefab.
  - أي تعديل على اللاعب = فتح هذا البريفاب مباشرة، ممنوع التدوير عليه بالمشهد.
  - الهرم: WhiteclownPlayer (Tag Player) > PlayerCamera (Tag MainCamera + Post-process Layer) ؛ mixamorig:Hips ؛ WhiteClown (SkinnedMeshRenderer).
  - المكونات بالجذر: Transform, Animator, CharacterController, CharController_Motor, PlayerInteraction, CameraViewSwitcher, StaminaSystem, PlayerSetup, PhotonView, PhotonTransformView, PhotonAnimatorView, FootstepSoundController, PlayerHealth, HeartbeatSfx, PlayerQuickChat.
  - الفلاش: mixamorig:Hips > Spine > Spine1 > Spine2 > RightShoulder > RightArm > RightForeArm > RightHand > Flashlight > Spotlight (عليه FlashlightSway.cs، محلي فقط).

• الوحش والـ Shaders:
  - موديل الزومبي: Assets/Monster/Model/Warzombie F Pedroso.fbx (Humanoid | Avatar: Warzombie F PedrosoAvatar).
  - التكستشرات: Assets/Monster/Model/world_war_zombie_diffuse, world_war_zombie_normal, world_war_zombie_specular.
  - المادة: Assets/Monster/Model/WorldWar_zombie_material.
  - SSR Shader: Assets/a.last/Flooded_Grounds/PostProcessing/Resources/Shaders/ScreenSpaceReflection.shader
  - TMP Shader: Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader (#pragma enable_debug_symbols).

====================================================================
6. إعدادات Photon والشبكة
====================================================================
Region: eu | App Version: 1.0 | Protocol: WSS | SendRate 20 | SerializationRate 10 (داخل ServerBrowser.Start)
PhotonNetwork.AutomaticallySyncScene = true قبل دخول أي Room.
Master Client فقط يتحكم بحركة وقرارات EnemyAI.
كل RpcTarget.All لازم يشتغل من 1 إلى 4 لاعبين.
قبل ضبط NickName تأكد أن الحالة مو Disconnecting/Leaving.

====================================================================
7. قواعد Unity 6.6 API
====================================================================
ممنوع FindObjectsSortMode | ممنوع FindFirstObjectByType.
استخدم FindObjectsByType<T>(FindObjectsInactive.Include) و FindAnyObjectByType<T>().
ممنوع GameObject.Find لعنصر ممكن يكون مطفي؛ الحقول تُحل Runtime.
إذا وُجد using System أضف بعده: using Object = UnityEngine.Object;
الخط الافتراضي: Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
ممنوع Projector | ممنوع Light.drawHalo.
ممنوع AppDomain.GetAssemblies داخل كود Editor (تحذير UAC0005) — استخدم CompilationPipeline أو فحص المجلدات.

====================================================================
8. قواعد التصميم والواجهة الدائمة (لا تتغير)
====================================================================
• اللعبة لازم تشتغل Online و Offline بدون أي مشكلة. كل ميزة تشتغل بلا إنترنت وبلا Photon room، وكذلك داخل room بـ 1–4 لاعبين.
  ممنوع استدعاء Photon APIs تفشل وقت عدم الاتصال؛ احمِ بـ PhotonNetwork.InRoom / OfflineMode.
• الواجهة لازم تكون موجودة بالإيديتور داخل الكانفس (ظاهرة وقابلة للتعديل بالـ Hierarchy/Scene)، مو مبنية Runtime فقط.
• كل أيقونة PNG جديدة: Texture Type = Sprite (2D and UI)، Sprite Mode = Single، ثم Apply — اذكرها للمستخدم كل مرة.
• أزرار HUD: دائرية وبنفس ستايل الرعب المعتمد (مولّد Palewick/Tools/hud_button_style.py، أيقونات MDI/FA مجانية، ممنوع فن PUBG).
• HUD لازم يكون نفسه بكل الأجهزة: العناصر مربوطة بأقرب زاوية شاشة وبحجم ثابت (Palewick/Lock HUD To Screen Corners).
• تبويبات الإعدادات الجانبية دائماً على اليمين بكل اللغات (محتوى الصفحات بس ينعكس للعربي/الكردي).
• Canvas Scaler: Scale With Screen Size 1920x1080، Match = 1 (Height).

====================================================================
9. نظام النقاط + Unity Cloud + AdMob (مكتمل وشغّال)
====================================================================
الحزم: com.unity.services.authentication + com.unity.services.cloudsave (منصّبة).
مزوّد الدخول المفعّل بلوحة Unity: Username & Password (Enabled). Google Play Games مؤجل لحد Play Console.

PwCloud.cs
  - تهيئة UnityServices + ResumeAsync (يستخدم الجلسة المخزونة فقط، ممنوع أي Anonymous).
  - SignInEmailAsync: الإيميل يتحول ليوزرنيم Unity (الإيميل نفسه إذا ≤20 حرف صالح، وإلا "pw" + 18 hex من SHA256).
  - الباسوورد: 8–30 حرف مع حرف كبير وصغير ورقم ورمز.
  - SignInGoogleAsync: كود GPGS → SignInWithGooglePlayGamesAsync (يحتاج PW_GPGS).
  - LoadIntAsync / SaveIntAsync على Cloud Save Data.Player.
  - مفاتيح: pw_account, pw_account_type.

PwPoints.cs
  - مفتاح Cloud Save: "points". لاعب جديد = 3 نقاط (StartPoints). RespawnCost = 1.
  - كاش محلي: pw_points_<playerId> + دلتا معلّقة pw_pending_<playerId>.
  - أوفلاين: التغييرات تنحفظ بالـ pending وتنرفع للسيرفر أول ما يرجع الاتصال (cloud + pending، إعادة محاولة كل 6 ثواني).
  - API: Points, Synced, CanRespawn, Add(int), TrySpend(int), event Changed(int).

PointPickup.cs
  - أوبجكت بالمشهد: PhotonView + SphereCollider (Trigger) + Visual + Glow.
  - يُلقط بالمرور فوقه (autoPickup) أو بزر التفاعل (IInteractable).
  - RPC_Take(actorNumber) بـ RpcTarget.AllBufferedViaServer → الغرض ينلقط مرة وحدة بالغرفة، والنقطة للاعب اللي طالبها أول.
  - أوفلاين ينطي النقطة محلياً.

PwAds.cs (AdMob Rewarded)
  - يُجمَّع فقط عند تعريف PW_ADMOB.
  - UseTestAd = true → إعلان Google التجريبي ca-app-pub-3940256099942544/5224354917
  - AndroidRewardedId (الحقيقي) = ca-app-pub-9194615148813735/8226703242
  - App ID (بإعدادات البلَكن) = ca-app-pub-9194615148813735~9623545392
  - API: Available / Ready / Preload() / Show(Action<bool>) — يستخدم MobileAdsEventExecutor.
  - الإعلان ما يظهر داخل الإيديتور أبداً، لازم APK على الموبايل.

DeathScreen.cs
  - Respawn يصرف نقطة وحدة (ترجع إذا فشل الإحياء) ويتعطل إذا النقاط < 1.
  - زر Watch Ad: الإعلان ينطي +1 ثم يحيي اللاعب (صافي 0).
  - يعرض عداد النقاط + سطر "إعادة الظهور تكلف نقطة وحدة" + سطر رسائل (Not enough points / Ad not ready).

الواجهات المبنية بالإيديتور
  - اللوبي (Palewick/Build Login Screen): LanguagePanel, LoginPanel, StartModePanel, PointsBadge, AccountBar.
  - Scene_A (Palewick/Build Points HUD): PointsBadge بأعلى الوسط (مستثنى من HudCornerLock).
  - Palewick/Create Point Pickup: ينشئ Pickup بمكان كاميرا المشهد (مادة Assets/UI_Lobby/PointPickupMat.mat).

====================================================================
10. شاشة اللغة + الدخول + أونلاين/أوفلاين
====================================================================
PwLanguageUI.cs: شاشة اختيار اللغة لأول مرة (کوردی / العربية / English). تخزن pw_lang و pw_lang_set=1.
PwAuthUI.cs: شاشة الدخول بالإيميل أو الجوجل. إذا الحساب مخزون تدخل مباشرة.
PwStartMode.cs: زر START باللوبي يفتح خيار Play Online أو Play Offline.

====================================================================
11. قائمة الإعدادات (PauseMenuPUBG.cs)
====================================================================
تخطيط ثابت ومحاذى بكل الشاشات: FrameW = 1480, FrameH = 1020 بوسط الشاشة PwRoot مع خلفية Grunge. SideW = 340 على اليمين. FitFrame() يصغر الإطار بالتساوي للشاشات الضيقة.

====================================================================
12. PlayerHealth / EnemyAI / Monster_AI
====================================================================
PlayerHealth: maxHealth 100، invincibilityTime 1، IsDead، Revive() مزامَن. الواجهة للاعب المحلي فقط.
EnemyAI: حالات Idle, Patrol, Investigate, Search, Chase, Return.
  السمع: ركض 16م، قفز 10م، مشي 4.5م، أبواب 12م (EnemyAI.HearNoise).
  يفقد الهدف بعد 3 ثواني بلا رؤية → يفحص آخر مكان → يبحث 5 ثواني → يرجع.
  يفتح الأبواب المغلقة أمامه (DoorController.OpenFrom). ضوء الفلاش عليه ضمن 9م/22° يبطّئ المطاردة x0.7.
  المستر فقط يحرّك؛ الباقي Lerp. الضرر عبر RPC_ApplyDamage.
Monster_AI بالمشهد: ViewID 133، Ownership Fixed، Observed = EnemyAI، Avatar = Warzombie F PedrosoAvatar.

====================================================================
13. الحركة والكاميرا (ممنوع تغييرها بدون طلب)
====================================================================
CharController_Motor: Move 3.5 | Sprint 6.5 | Accel 8 | Decel 12 | Gravity -19.62
Animator اللاعب: Speed (Float) — Idle 0 / Walk 0.5 / Run 1، عتبات 0.25 و 0.75، Has Exit Time مطفأ.
CameraViewSwitcher: FP Height 1.7 | TP Distance 3.5 | TP Height 1.5 | FP FOV 60 | TP FOV 70 | Pitch -30..60.

====================================================================
14. المنجز والمعلّق وحالة الجهاز
====================================================================
المنجز: نظام النقاط، Unity Cloud، AdMob، شاشة اللغة والدخول، إصلاح الإعدادات، تنظيف تحذيرات الإيديتور والـ Plugins.
المعلّق:
1. إكمال معلومات الدفع بـ AdMob للإعلانات الحقيقية.
2. تبديل UseTestAd إلى false بملف PwAds.cs عند الرفع النهائي.
3. ربط Google Play Games عند توفر Play Console.
4. PostProcessing Runtime/Models (مؤجل).
جهاز المستخدم: Dell Latitude E7240, i5-4310U, 8 GB DDR3, القرص C: ~52 GB مجانية.

====================================================================
15. تحديث V33 — الرعب والتحسينات الشاملة
====================================================================
• الخريطة الكاملة صارت بستايل PUBG التكتيكي: MinimapBuilder يبني FullMapPanel جديد (إطار داكن + هيدر TACTICAL MAP + شبكة Grid عالمية + أزرار زوم/إغلاق أنيقة + Legend + زوايا ذهبية). حقول جديدة بـ Minimap.cs: fullMapGrid و fullMapScaleText (SerializeField). ملف الشبكة: Assets/UI_Icons/map_grid.png (يولده البلدر).
• إعدادات الرعب الموحدة: PwHorrorSettingsFx.cs ينضاف تلقائياً على PausePanel بالمشهدين (لوبي + Scene_A) — ظلال جانبية، Jumpscare خاطف نادر عند تبديل التبويبات، نبضة Vignette حمراء عند تحريك السلايدرات.
• الانترو: IntroManager صار بيه flickerGroup + glowTitle (PALEWICK متوهج نابض) + shadowDrift (ظل يمشي)، والانتقال للوبي فيد ناعم 0.6 ثانية. IntroBuilder يبنيها كلها (Palewick/Build Horror Intro). ملف الظل: Assets/UI_Lobby/intro_shadow.png.
• الأداء: PwPerformance.cs (60 FPS + vSync 0 + ضبط تلقائي للأجهزة الضعيفة بأول تشغيل) + أداة Palewick/Optimize Mobile Performance.
• المواد المفقودة: أداة Palewick/Fix Missing Materials تصلح المجسمات البيضاء (الأكواخ بـ Scene_A) وتعيد Standard Shader، والاحتياطي بـ Assets/PwFixedMaterials/PwFallbackWood.mat.
• الأيقونات: كل أيقونات HUD انستبدلت بأيقونات رعب (نفس الملفات/GUIDs بـ Assets/UI_Icons) + أيقونة جديدة hud_crouch.png. المولد: Tools/horror_icons.py.
• المتجر الجديد: انحذفت ألوان اللاعب نهائياً. الكتالوج: سكنات رعب (Pale Clown / The Butcher / The Wraith / Plague Doctor) + سكنات كشاف (Warm / Cursed / Spectral مع فليكر) + معدات بقاء (Warding Talisman يبطئ الوحش أكثر بالضوء، Adrenaline Shot ستامينا +25%، Lucky Charm فرصة 30% إحياء مجاني). أيقونات جديدة بـ Assets/UI_Lobby/Shop. بعد السحب لازم تشغيل Palewick/Build Shop وحفظ Scene_Lobby.
• أدوات لازم تنشغل بالإيديتور بعد السحب: Build Shop (لوبي) + Create Minimap In Canvas (حذف MinimapRoot القديم أولاً بـ Scene_A) + Build Horror Intro + Fix Missing Materials + Optimize Mobile Performance.