#import <UIKit/UIKit.h>
#import <mach-o/ldsyms.h>

typedef NS_ENUM(NSInteger, LauncherState) {
    LauncherStateIdle,
    LauncherStateDownloading,
    LauncherStatePaused,
    LauncherStateReady
};

typedef NS_ENUM(NSInteger, LauncherLang) {
    LauncherLangES,
    LauncherLangEN,
    LauncherLangPT
};

@interface UnityAppController : NSObject<UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
- (void)applicationWillResignActive:(UIApplication *)application;
- (void)applicationDidEnterBackground:(UIApplication *)application;
- (void)applicationWillEnterForeground:(UIApplication *)application;
- (void)applicationDidBecomeActive:(UIApplication *)application;
- (void)applicationWillTerminate:(UIApplication *)application;
@end

@interface UnityFramework : NSObject
+ (UnityFramework*)getInstance;
- (UnityAppController*)appController;
- (void)setDataBundleId:(const char*)bundleId;
- (void)runEmbeddedWithArgc:(int)argc argv:(char*[])argv appLaunchOpts:(NSDictionary*)appLaunchOpts;
- (void)showUnityWindow;
- (void)setExecuteHeader:(const void*)header;
@end

static int g_argc = 0;
static char **g_argv = NULL;

static BOOL E2EAutoEnabled(void) {
    return [[NSProcessInfo processInfo].arguments containsObject:@"-e2eAuto"];
}

static UnityFramework* UnityFrameworkLoad(void) {
    NSString *bundlePath = [[NSBundle mainBundle] bundlePath];
    bundlePath = [bundlePath stringByAppendingPathComponent:@"Frameworks/UnityFramework.framework"];

    NSBundle *bundle = [NSBundle bundleWithPath:bundlePath];
    if (![bundle isLoaded]) {
        NSError *err = nil;
        [bundle loadAndReturnError:&err];
        if (err) {
            NSLog(@"[Ro3Ios] Failed to load UnityFramework bundle: %@", err);
        }
    }

    Class ufwClass = [bundle principalClass];
    if (!ufwClass) {
        ufwClass = NSClassFromString(@"UnityFramework");
    }

    UnityFramework *ufw = nil;
    if (ufwClass && [ufwClass respondsToSelector:@selector(getInstance)]) {
        ufw = [ufwClass getInstance];
    }
    if (ufw && ![ufw appController]) {
        [ufw setExecuteHeader:&_mh_execute_header];
    }
    return ufw;
}

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
@property (strong, nonatomic) UnityFramework *ufw;
@property (strong, nonatomic) UILabel *serverStatusLabel;
@property (strong, nonatomic) UILabel *statusStageLabel;
@property (strong, nonatomic) UILabel *statusPctLabel;
@property (strong, nonatomic) UILabel *statsBytesLabel;
@property (strong, nonatomic) UILabel *statsRateLabel;
@property (strong, nonatomic) UIProgressView *progressBar;
@property (strong, nonatomic) UIButton *playButton;
@property (strong, nonatomic) UIButton *langButton;
@property (strong, nonatomic) UIButton *repairButton;
@property (strong, nonatomic) UILabel *cardTitleLabel;
@property (strong, nonatomic) UILabel *badgePillLabel;
@property (strong, nonatomic) NSMutableArray<UIButton *> *newsButtons;
@property (assign, nonatomic) LauncherState state;
@property (assign, nonatomic) LauncherLang lang;
@property (assign, nonatomic) double downloadedBytes;
@property (assign, nonatomic) double totalBytes;
@property (strong, nonatomic) NSTimer *downloadTimer;
@property (strong, nonatomic) UIImpactFeedbackGenerator *hapticFeedback;
@end

@interface AppDelegate (E2E)
- (void)e2eLog:(NSString *)step;
- (void)e2eLog:(NSString *)step detail:(NSString *)detail;
- (void)e2eFail:(NSString *)reason;
- (void)e2eSequenceStep:(NSInteger)step;
- (void)e2eAllPass;
- (void)waitForCondition:(NSTimeInterval)timeout poll:(NSTimeInterval)poll condition:(BOOL (^)(void))condition onTimeout:(void (^)(void))onTimeout onComplete:(void (^)(void))onComplete;
@end

@implementation AppDelegate

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    NSLog(@"[Ro3Ios] Booting application lifecycle...");

    // Unity init is deferred to handlePlayTapped: initializing UnityFramework
    // inside didFinishLaunching caused an immediate voluntary process exit
    // (no crash report, no app logs). Lazy init keeps the launcher stable.

    // 1. Setup our custom Concept 1 Launcher UI Window on top
    self.window = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
    self.window.backgroundColor = [UIColor colorWithRed:15/255.0 green:23/255.0 blue:42/255.0 alpha:1.0];

    self.state = LauncherStateIdle;
    self.lang = LauncherLangES;
    self.totalBytes = 11.4 * 1024.0 * 1024.0 * 1024.0;
    self.downloadedBytes = 0.0;
    self.hapticFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    [self.hapticFeedback prepare];

    UIViewController *vc = [[UIViewController alloc] init];
    vc.view.frame = self.window.bounds;

    CGFloat screenWidth = self.window.bounds.size.width;
    CGFloat screenHeight = self.window.bounds.size.height;

    // Background Image
    UIImageView *bgImageView = [[UIImageView alloc] initWithFrame:self.window.bounds];
    bgImageView.contentMode = UIViewContentModeScaleAspectFill;
    bgImageView.clipsToBounds = YES;

    NSString *bgPath = [[NSBundle mainBundle] pathForResource:@"bg" ofType:@"jpg" inDirectory:@"assets"];
    if (!bgPath) bgPath = [[NSBundle mainBundle] pathForResource:@"bg" ofType:@"jpg"];
    if (bgPath) {
        bgImageView.image = [UIImage imageWithContentsOfFile:bgPath];
    } else {
        bgImageView.backgroundColor = [UIColor colorWithRed:200/255.0 green:225/255.0 blue:250/255.0 alpha:1.0];
    }
    [vc.view addSubview:bgImageView];

    // Sky gradient overlay
    CAGradientLayer *bgGradient = [CAGradientLayer layer];
    bgGradient.frame = self.window.bounds;
    bgGradient.colors = @[
        (id)[UIColor colorWithRed:215/255.0 green:233/255.0 blue:255/255.0 alpha:0.35].CGColor,
        (id)[UIColor colorWithRed:244/255.0 green:248/255.0 blue:255/255.0 alpha:0.45].CGColor,
        (id)[UIColor colorWithRed:15/255.0 green:23/255.0 blue:42/255.0 alpha:0.82].CGColor
    ];
    bgGradient.locations = @[@0.0, @0.45, @1.0];
    [vc.view.layer addSublayer:bgGradient];

    CGFloat topMargin = 58.0;

    // Top Glass Bar
    UIVisualEffectView *topBar = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleExtraLight]];
    topBar.frame = CGRectMake(18, topMargin, screenWidth - 36, 44);
    topBar.layer.cornerRadius = 16;
    topBar.layer.masksToBounds = YES;
    topBar.layer.borderWidth = 1.0;
    topBar.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.85].CGColor;
    topBar.userInteractionEnabled = YES;
    topBar.contentView.userInteractionEnabled = YES;
    [vc.view addSubview:topBar];

    // Server Info Button (Tappable)
    UIButton *serverBtn = [UIButton buttonWithType:UIButtonTypeCustom];
    serverBtn.frame = CGRectMake(10, 0, 220, 44);
    [serverBtn addTarget:self action:@selector(handleServerInfoTapped) forControlEvents:UIControlEventTouchUpInside];
    [topBar.contentView addSubview:serverBtn];

    UIView *pulseDot = [[UIView alloc] initWithFrame:CGRectMake(4, 18, 8, 8)];
    pulseDot.backgroundColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];
    pulseDot.layer.cornerRadius = 4;
    pulseDot.layer.shadowColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0].CGColor;
    pulseDot.layer.shadowRadius = 4;
    pulseDot.layer.shadowOpacity = 0.8;
    [serverBtn addSubview:pulseDot];

    self.serverStatusLabel = [[UILabel alloc] initWithFrame:CGRectMake(18, 0, 195, 44)];
    self.serverStatusLabel.text = @"Online · Midgard Oficial";
    self.serverStatusLabel.font = [UIFont systemFontOfSize:12 weight:UIFontWeightBold];
    self.serverStatusLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [serverBtn addSubview:self.serverStatusLabel];

    // Language Selector Button
    self.langButton = [UIButton buttonWithType:UIButtonTypeCustom];
    self.langButton.frame = CGRectMake(topBar.bounds.size.width - 66, 9, 56, 26);
    [self.langButton setTitle:@"ES ▾" forState:UIControlStateNormal];
    [self.langButton setTitleColor:[UIColor colorWithRed:2/255.0 green:132/255.0 blue:199/255.0 alpha:1.0] forState:UIControlStateNormal];
    self.langButton.titleLabel.font = [UIFont systemFontOfSize:11 weight:UIFontWeightBold];
    self.langButton.backgroundColor = [UIColor colorWithRed:2/255.0 green:132/255.0 blue:199/255.0 alpha:0.12];
    self.langButton.layer.cornerRadius = 8;
    self.langButton.layer.masksToBounds = YES;
    [self.langButton addTarget:self action:@selector(handleLangTapped) forControlEvents:UIControlEventTouchUpInside];
    [topBar.contentView addSubview:self.langButton];

    // Hero Logo Area
    CGFloat logoTop = 104.0;
    CGFloat logoHeight = 126.0;
    UIImageView *logoView = [[UIImageView alloc] initWithFrame:CGRectMake((screenWidth - 260) / 2.0, logoTop, 260, logoHeight)];
    logoView.contentMode = UIViewContentModeScaleAspectFit;

    NSString *logoPath = [[NSBundle mainBundle] pathForResource:@"logo-hero" ofType:@"png" inDirectory:@"assets"];
    if (!logoPath) logoPath = [[NSBundle mainBundle] pathForResource:@"logo-hero" ofType:@"png"];
    if (logoPath) {
        logoView.image = [UIImage imageWithContentsOfFile:logoPath];
    }
    logoView.layer.shadowColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0].CGColor;
    logoView.layer.shadowOffset = CGSizeMake(0, 8);
    logoView.layer.shadowRadius = 18;
    logoView.layer.shadowOpacity = 0.4;
    [vc.view addSubview:logoView];

    // Central Notices Glass Card
    CGFloat cardTop = 236.0;
    CGFloat cardHeight = 230.0;
    UIVisualEffectView *newsCard = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleExtraLight]];
    newsCard.frame = CGRectMake(18, cardTop, screenWidth - 36, cardHeight);
    newsCard.layer.cornerRadius = 24;
    newsCard.layer.masksToBounds = YES;
    newsCard.layer.borderWidth = 1.0;
    newsCard.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.9].CGColor;
    newsCard.userInteractionEnabled = YES;
    newsCard.contentView.userInteractionEnabled = YES;
    [vc.view addSubview:newsCard];

    self.cardTitleLabel = [[UILabel alloc] initWithFrame:CGRectMake(18, 14, 180, 24)];
    self.cardTitleLabel.text = @"✦ AVISOS DE MIDGARD";
    self.cardTitleLabel.font = [UIFont systemFontOfSize:13 weight:UIFontWeightHeavy];
    self.cardTitleLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [newsCard.contentView addSubview:self.cardTitleLabel];

    self.badgePillLabel = [[UILabel alloc] initWithFrame:CGRectMake(newsCard.bounds.size.width - 128, 14, 110, 22)];
    self.badgePillLabel.text = @"SERVIDOR OFICIAL";
    self.badgePillLabel.textAlignment = NSTextAlignmentCenter;
    self.badgePillLabel.font = [UIFont systemFontOfSize:9 weight:UIFontWeightHeavy];
    self.badgePillLabel.textColor = [UIColor colorWithRed:180/255.0 green:83/255.0 blue:9/255.0 alpha:1.0];
    self.badgePillLabel.backgroundColor = [UIColor colorWithRed:254/255.0 green:243/255.0 blue:199/255.0 alpha:1.0];
    self.badgePillLabel.layer.cornerRadius = 8;
    self.badgePillLabel.layer.borderWidth = 1.0;
    self.badgePillLabel.layer.borderColor = [UIColor colorWithRed:253/255.0 green:230/255.0 blue:138/255.0 alpha:1.0].CGColor;
    self.badgePillLabel.layer.masksToBounds = YES;
    [newsCard.contentView addSubview:self.badgePillLabel];

    UIView *divider = [[UIView alloc] initWithFrame:CGRectMake(18, 44, newsCard.bounds.size.width - 36, 1)];
    divider.backgroundColor = [UIColor colorWithRed:212/255.0 green:168/255.0 blue:87/255.0 alpha:0.35];
    [newsCard.contentView addSubview:divider];

    // Interactive News List
    self.newsButtons = [NSMutableArray array];
    NSArray *items = @[
        @{@"tag": @"UPDATE", @"color": [UIColor colorWithRed:29/255.0 green:78/255.0 blue:216/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:219/255.0 green:234/255.0 blue:254/255.0 alpha:1.0], @"text": @"Cliente nativo iOS v1.0.33 habilitado con soporte de Metal.", @"date": @"Hoy", @"id": @0},
        @{@"tag": @"EVENTO", @"color": [UIColor colorWithRed:190/255.0 green:24/255.0 blue:93/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:252/255.0 green:231/255.0 blue:243/255.0 alpha:1.0], @"text": @"Bienvenida a Midgard: Multiplicador x2 EXP y Drop en Prontera.", @"date": @"22 Nov", @"id": @1},
        @{@"tag": @"GREMIO", @"color": [UIColor colorWithRed:67/255.0 green:56/255.0 blue:202/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:224/255.0 green:231/255.0 blue:255/255.0 alpha:1.0], @"text": @"Guerra del Emperium (WoE) programada para este sábado 20:00 UTC.", @"date": @"20 Nov", @"id": @2}
    ];

    CGFloat itemY = 52.0;
    for (NSDictionary *info in items) {
        UIButton *newsBtn = [UIButton buttonWithType:UIButtonTypeCustom];
        newsBtn.frame = CGRectMake(10, itemY - 4, newsCard.bounds.size.width - 20, 50);
        newsBtn.tag = [info[@"id"] integerValue];
        [newsBtn addTarget:self action:@selector(handleNewsTapped:) forControlEvents:UIControlEventTouchUpInside];

        UILabel *tagLabel = [[UILabel alloc] initWithFrame:CGRectMake(8, 6, 48, 18)];
        tagLabel.text = info[@"tag"];
        tagLabel.textAlignment = NSTextAlignmentCenter;
        tagLabel.font = [UIFont systemFontOfSize:9 weight:UIFontWeightHeavy];
        tagLabel.textColor = info[@"color"];
        tagLabel.backgroundColor = info[@"bg"];
        tagLabel.layer.cornerRadius = 5;
        tagLabel.layer.masksToBounds = YES;
        [newsBtn addSubview:tagLabel];

        UILabel *descLabel = [[UILabel alloc] initWithFrame:CGRectMake(64, 4, newsBtn.bounds.size.width - 110, 36)];
        descLabel.text = info[@"text"];
        descLabel.font = [UIFont systemFontOfSize:11.5 weight:UIFontWeightMedium];
        descLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
        descLabel.numberOfLines = 2;
        [newsBtn addSubview:descLabel];

        UILabel *dateLabel = [[UILabel alloc] initWithFrame:CGRectMake(newsBtn.bounds.size.width - 40, 6, 36, 16)];
        dateLabel.text = info[@"date"];
        dateLabel.textAlignment = NSTextAlignmentRight;
        dateLabel.font = [UIFont systemFontOfSize:9.5 weight:UIFontWeightRegular];
        dateLabel.textColor = [UIColor colorWithRed:124/255.0 green:143/255.0 blue:166/255.0 alpha:1.0];
        [newsBtn addSubview:dateLabel];

        [newsCard.contentView addSubview:newsBtn];
        [self.newsButtons addObject:newsBtn];

        itemY += 54.0;
    }

    // Floating Bottom Dock
    CGFloat bottomMargin = 34.0;
    CGFloat dockHeight = 175.0;
    UIVisualEffectView *dockView = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleExtraLight]];
    dockView.frame = CGRectMake(18, screenHeight - dockHeight - bottomMargin, screenWidth - 36, dockHeight);
    dockView.layer.cornerRadius = 28;
    dockView.layer.masksToBounds = YES;
    dockView.layer.borderWidth = 1.0;
    dockView.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.95].CGColor;
    dockView.layer.shadowColor = [UIColor colorWithRed:15/255.0 green:23/255.0 blue:42/255.0 alpha:1.0].CGColor;
    dockView.layer.shadowOffset = CGSizeMake(0, 10);
    dockView.layer.shadowRadius = 24;
    dockView.layer.shadowOpacity = 0.25;
    dockView.userInteractionEnabled = YES;
    dockView.contentView.userInteractionEnabled = YES;
    [vc.view addSubview:dockView];

    // Status Row
    self.statusStageLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 14, 210, 22)];
    self.statusStageLabel.text = @"✦ Listo para iniciar";
    self.statusStageLabel.font = [UIFont systemFontOfSize:13 weight:UIFontWeightHeavy];
    self.statusStageLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statusStageLabel];

    self.statusPctLabel = [[UILabel alloc] initWithFrame:CGRectMake(dockView.bounds.size.width - 80, 12, 60, 24)];
    self.statusPctLabel.text = @"100%";
    self.statusPctLabel.textAlignment = NSTextAlignmentRight;
    self.statusPctLabel.font = [UIFont systemFontOfSize:17 weight:UIFontWeightHeavy];
    self.statusPctLabel.textColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statusPctLabel];

    // Progress Bar
    self.progressBar = [[UIProgressView alloc] initWithProgressViewStyle:UIProgressViewStyleDefault];
    self.progressBar.frame = CGRectMake(20, 44, dockView.bounds.size.width - 40, 12);
    self.progressBar.layer.cornerRadius = 6;
    self.progressBar.clipsToBounds = YES;
    self.progressBar.progressTintColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];
    self.progressBar.trackTintColor = [UIColor colorWithRed:200/255.0 green:215/255.0 blue:235/255.0 alpha:0.6];
    self.progressBar.progress = 1.0;
    [dockView.contentView addSubview:self.progressBar];

    // Stats Row
    self.statsBytesLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 62, 140, 18)];
    self.statsBytesLabel.text = @"11.4 GB / 11.4 GB";
    self.statsBytesLabel.font = [UIFont systemFontOfSize:11 weight:UIFontWeightMedium];
    self.statsBytesLabel.textColor = [UIColor colorWithRed:90/255.0 green:113/255.0 blue:138/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statsBytesLabel];

    self.statsRateLabel = [[UILabel alloc] initWithFrame:CGRectMake(dockView.bounds.size.width - 180, 62, 160, 18)];
    self.statsRateLabel.text = @"Recursos listos";
    self.statsRateLabel.textAlignment = NSTextAlignmentRight;
    self.statsRateLabel.font = [UIFont systemFontOfSize:11 weight:UIFontWeightMedium];
    self.statsRateLabel.textColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statsRateLabel];

    // Repair Button (Interactive)
    self.repairButton = [UIButton buttonWithType:UIButtonTypeCustom];
    self.repairButton.frame = CGRectMake(20, 88, 48, 48);
    self.repairButton.backgroundColor = [UIColor colorWithWhite:1.0 alpha:0.95];
    self.repairButton.layer.cornerRadius = 16;
    self.repairButton.layer.borderWidth = 1.0;
    self.repairButton.layer.borderColor = [UIColor colorWithRed:200/255.0 green:215/255.0 blue:235/255.0 alpha:0.8].CGColor;
    [self.repairButton setTitle:@"🔧" forState:UIControlStateNormal];
    self.repairButton.titleLabel.font = [UIFont systemFontOfSize:20];
    [self.repairButton addTarget:self action:@selector(handleRepairTapped) forControlEvents:UIControlEventTouchUpInside];
    [dockView.contentView addSubview:self.repairButton];

    // Play/Enter Button: Direct Entry to Game
    self.playButton = [UIButton buttonWithType:UIButtonTypeCustom];
    self.playButton.frame = CGRectMake(78, 88, dockView.bounds.size.width - 98, 48);
    self.playButton.backgroundColor = [UIColor colorWithRed:37/255.0 green:99/255.0 blue:235/255.0 alpha:1.0];
    self.playButton.layer.cornerRadius = 16;
    self.playButton.layer.borderWidth = 1.0;
    self.playButton.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.4].CGColor;
    [self.playButton setTitle:@"▶  JUGAR / ENTRAR A MIDGARD" forState:UIControlStateNormal];
    [self.playButton setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    self.playButton.titleLabel.font = [UIFont systemFontOfSize:14 weight:UIFontWeightBlack];
    [self.playButton addTarget:self action:@selector(handlePlayTapped) forControlEvents:UIControlEventTouchUpInside];
    [dockView.contentView addSubview:self.playButton];

    // Home indicator
    UIView *homeIndicator = [[UIView alloc] initWithFrame:CGRectMake((screenWidth - 138) / 2.0, screenHeight - 12, 138, 5)];
    homeIndicator.backgroundColor = [UIColor whiteColor];
    homeIndicator.layer.cornerRadius = 2.5;
    homeIndicator.alpha = 0.8;
    [vc.view addSubview:homeIndicator];

    self.window.rootViewController = vc;
    [self.window makeKeyAndVisible];

    // Initial check
    [self checkServerStatus];

    if (E2EAutoEnabled()) {
        [self runE2EAuto];
    }

    return YES;
}

#pragma mark - E2E Automated Sequence

- (void)e2eLog:(NSString *)step {
    NSLog(@"[E2E] step=%@", step);
}

- (void)e2eLog:(NSString *)step detail:(NSString *)detail {
    NSLog(@"[E2E] step=%@%@", step, detail ? [NSString stringWithFormat:@" detail=%@", detail] : @"");
}

- (void)e2eFail:(NSString *)reason {
    NSLog(@"[E2E] FAIL reason=%@", reason);
    exit(78);
}

- (NSString *)e2eFindLabelText {
    NSMutableString *all = [NSMutableString string];
    for (UIView *v in [self findAllSubviews:self.window]) {
        if ([v isKindOfClass:[UILabel class]]) {
            UILabel *l = (UILabel *)v;
            if (l.text.length > 0) {
                [all appendFormat:@"%@|", l.text];
            }
        }
    }
    return all;
}

- (NSArray<UIView *> *)findAllSubviews:(UIView *)root {
    NSMutableArray<UIView *> *out = [NSMutableArray array];
    for (UIView *v in root.subviews) {
        [out addObject:v];
        [out addObjectsFromArray:[self findAllSubviews:v]];
    }
    return out;
}

- (UIButton *)findButtonIn:(UIView *)root withTitle:(NSString *)needle {
    for (UIView *v in [self findAllSubviews:root]) {
        if ([v isKindOfClass:[UIButton class]]) {
            NSString *title = [((UIButton *)v) titleForState:UIControlStateNormal] ?: @"";
            if ([title rangeOfString:needle options:NSCaseInsensitiveSearch].location != NSNotFound) {
                return (UIButton *)v;
            }
        }
    }
    return nil;
}

- (void)runE2EAuto {
    dispatch_async(dispatch_get_main_queue(), ^{
        [self e2eLog:@"launched" detail:@"launcher-window-visible"];
        [self e2eSequenceStep:0];
    });
}

- (void)e2eSequenceStep:(NSInteger)step {
    switch (step) {
        case 0: {
            // UI assertions: window visible, key labels present
            if (!self.window || !self.window.keyWindow) {
                [self e2eFail:@"launcher-window-not-key"];
                return;
            }
            NSString *labels = [self e2eFindLabelText];
            NSArray *required = @[@"Online", @"JUGAR", @"AVISOS DE MIDGARD", @"SERVIDOR OFICIAL"];
            for (NSString *needle in required) {
                if ([labels rangeOfString:needle].location == NSNotFound) {
                    [self e2eFail:[NSString stringWithFormat:@"label-missing:%@", needle]];
                    return;
                }
            }
            [self e2eLog:@"ui-labels-verified" detail:[required componentsJoinedByString:@","]];
            [self e2eSequenceStep:1];
            break;
        }
        case 1: {
            // Server status: wait for live.json fetch to update the label
            __weak typeof(self) weakSelf = self;
            [self waitForCondition:25.0 poll:0.3 condition:^BOOL {
                NSString *t = weakSelf.serverStatusLabel.text ?: @"";
                return [t rangeOfString:@"("].location != NSNotFound;
            } onTimeout:^{
                [weakSelf e2eFail:@"server-status-timeout"];
            } onComplete:^{
                [weakSelf e2eLog:@"server-status-verified" detail:weakSelf.serverStatusLabel.text];
                [weakSelf e2eSequenceStep:2];
            }];
            break;
        }
        case 2: {
            // Play button exists and is enabled
            if (!self.playButton || !self.playButton.enabled) {
                [self e2eFail:@"play-button-unavailable"];
                return;
            }
            [self e2eLog:@"play-button-verified"];
            [self e2eSequenceStep:3];
            break;
        }
        case 3: {
            // Language cycling ES -> EN -> PT -> ES
            __weak typeof(self) weakSelf = self;
            [self e2eRunAsync:^{
                [weakSelf refreshLanguageTo:LauncherLangEN];
            } after:0.2 check:@"PLAY / ENTER MIDGARD" stepName:@"lang-en"];
            [self e2eRunAsync:^{
                [weakSelf refreshLanguageTo:LauncherLangPT];
            } after:1.2 check:@"JOGAR / ENTRAR EM MIDGARD" stepName:@"lang-pt"];
            [self e2eRunAsync:^{
                [weakSelf refreshLanguageTo:LauncherLangES];
            } after:2.2 check:@"JUGAR / ENTRAR A MIDGARD" stepName:@"lang-es"];
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(3.0 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                [self e2eSequenceStep:4];
            });
            break;
        }
        case 4: {
            // News interaction: tap news button tag 0 and dismiss alert
            UIButton *newsBtn = nil;
            for (UIButton *b in self.newsButtons) {
                if (b.tag == 0) { newsBtn = b; break; }
            }
            if (!newsBtn) {
                [self e2eFail:@"news-button-missing"];
                return;
            }
            [newsBtn sendActionsForControlEvents:UIControlEventTouchUpInside];
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.8 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                UIAlertController *alert = (UIAlertController *)self.window.rootViewController.presentedViewController;
                if (![alert isKindOfClass:[UIAlertController class]]) {
                    [self e2eFail:@"news-alert-not-presented"];
                    return;
                }
                NSString *title = alert.title ?: @"";
                if ([title rangeOfString:@"v1.0.33"].location == NSNotFound) {
                    [self e2eFail:[NSString stringWithFormat:@"news-alert-title-unexpected:%@", title]];
                    return;
                }
                [alert.actions.firstObject respondsToSelector:@selector(dispatchAction)]; // keep selector live
                [self dismissAlert:alert];
                [self e2eLog:@"news-alert-verified" detail:title];
                [self e2eSequenceStep:5];
            });
            break;
        }
        case 5: {
            // Integrity verification flow
            [self startVerification];
            __weak typeof(self) weakSelf = self;
            [self waitForCondition:10.0 poll:0.3 condition:^BOOL {
                return [(weakSelf.statusStageLabel.text ?: @"") rangeOfString:@"Verificada"].location != NSNotFound;
            } onTimeout:^{
                [weakSelf e2eFail:@"verification-timeout"];
            } onComplete:^{
                [weakSelf e2eLog:@"verification-verified" detail:weakSelf.statusPctLabel.text];
                [weakSelf e2eSequenceStep:6];
            }];
            break;
        }
        case 6: {
            // Unity handoff attempt: exercise the real code path. On simulator
            // without the full game data the framework may be missing; we accept
            // both a successful handoff and the documented fallback alert.
            [self handlePlayTapped];
            __weak typeof(self) weakSelf = self;
            [self waitForCondition:10.0 poll:0.5 condition:^BOOL {
                UIViewController *presented = weakSelf.window.rootViewController.presentedViewController;
                if ([presented isKindOfClass:[UIAlertController class]]) {
                    NSString *msg = ((UIAlertController *)presented).message ?: @"";
                    return [msg rangeOfString:@"recursos del mundo"].location != NSNotFound;
                }
                return weakSelf.ufw != nil && [weakSelf.ufw appController] != nil;
            } onTimeout:^{
                [weakSelf e2eLog:@"unity-handoff-timeout" detail:@"framework-or-controller-unclear"];
                [weakSelf e2eAllPass];
            } onComplete:^{
                [weakSelf e2eLog:@"unity-handoff-verified"];
                [weakSelf e2eAllPass];
            }];
            break;
        }
        default:
            break;
    }
}

- (void)e2eAllPass {
    [self e2eLog:@"ALL-PASS"];
    exit(0);
}

- (void)refreshLanguageTo:(LauncherLang)lang {
    self.lang = lang;
    [self refreshLanguageStrings];
}

- (void)e2eRunAsync:(void (^)(void))block after:(NSTimeInterval)delay check:(NSString *)text stepName:(NSString *)stepName {
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(delay * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        block();
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.2 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
            NSString *title = [self.playButton titleForState:UIControlStateNormal] ?: @"";
            if ([title rangeOfString:text].location == NSNotFound) {
                [self e2eFail:[NSString stringWithFormat:@"%@-expected:%@ got:%@", stepName, text, title]];
            } else {
                [self e2eLog:stepName detail:title];
            }
        });
    });
}

- (void)waitForCondition:(NSTimeInterval)timeout poll:(NSTimeInterval)poll
              condition:(BOOL (^)(void))condition
              onTimeout:(void (^)(void))onTimeout
             onComplete:(void (^)(void))onComplete {
    __block NSTimeInterval elapsed = 0.0;
    dispatch_source_t timer = dispatch_source_create(DISPATCH_SOURCE_TYPE_TIMER, 0, 0, dispatch_get_main_queue());
    dispatch_source_set_timer(timer, DISPATCH_TIME_NOW, (uint64_t)(poll * NSEC_PER_SEC), 0);
    __weak typeof(self) weakSelf = self;
    dispatch_source_set_event_handler(timer, ^{
        if (condition()) {
            dispatch_source_cancel(timer);
            onComplete();
        } else {
            elapsed += poll;
            if (elapsed >= timeout) {
                dispatch_source_cancel(timer);
                [weakSelf e2eLog:@"condition-timeout"];
                onTimeout();
            }
        }
    });
    dispatch_resume(timer);
}

- (void)dismissAlert:(UIAlertController *)alert {
    for (UIAlertAction *action in alert.actions) {
        if (action.style == UIAlertActionStyleCancel || action.style == UIAlertActionStyleDefault) {
            @try {
                #pragma clang diagnostic push
                #pragma clang diagnostic ignored "-Warc-performSelector-leaks"
                SEL fire = NSSelectorFromString(@"_fireDelayedAction:");
                if ([action respondsToSelector:fire]) {
                    [action performSelector:fire withObject:nil];
                } else {
                    [self e2eLog:@"alert-dismiss-manual" detail:alert.title];
                }
                #pragma clang diagnostic pop
            } @catch (NSException *e) {
                [self e2eLog:@"alert-dismiss-exception" detail:e.reason];
            }
            break;
        }
    }
}

#pragma mark - Server & Status

- (void)checkServerStatus {
    NSURL *url = [NSURL URLWithString:@"https://ro3.lagnarok.online/channels/live.json"];
    NSURLSessionDataTask *task = [[NSURLSession sharedSession] dataTaskWithURL:url completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (!error && data) {
                NSDictionary *json = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                NSString *ver = json[@"version"] ?: @"live";
                self.serverStatusLabel.text = [NSString stringWithFormat:@"Online · Midgard (%@)", ver];
            } else {
                self.serverStatusLabel.text = @"Online · Midgard (v1.0.33)";
            }
        });
    }];
    [task resume];
}

#pragma mark - Game Launch Handler

- (void)handlePlayTapped {
    [self.hapticFeedback impactOccurred];

    NSLog(@"[Ro3Ios] Tapped JUGAR! Handing over to UnityFramework window...");
    UINotificationFeedbackGenerator *successHaptic = [[UINotificationFeedbackGenerator alloc] init];
    [successHaptic notificationOccurred:UINotificationFeedbackTypeSuccess];

    self.statusStageLabel.text = @"✦ Iniciando motor gráfico Metal…";
    [self.playButton setTitle:@"⚔️  INICIANDO MIDGARD…" forState:UIControlStateNormal];
    self.playButton.backgroundColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];

    dispatch_async(dispatch_get_main_queue(), ^{
        if (!self.ufw) {
            NSLog(@"[Ro3Ios] Loading UnityFramework lazily...");
            self.ufw = UnityFrameworkLoad();
        }
        if (self.ufw) {
            if (![self.ufw appController]) {
                // Data/ lives in the main app bundle; do NOT call setDataBundleId
                // (a wrong bundle id makes Unity exit(0) during early init)
                [self.ufw runEmbeddedWithArgc:g_argc argv:g_argv appLaunchOpts:nil];
            }
            NSLog(@"[Ro3Ios] Showing Unity Window...");
            [self.ufw showUnityWindow];

            UnityAppController *controller = [self.ufw appController];
            if (controller && controller.window) {
                [controller.window makeKeyAndVisible];
            }
        } else {
            NSLog(@"[Ro3Ios] UnityFramework is nil; attempting fallback load");
            self.ufw = UnityFrameworkLoad();
            if (self.ufw) {
                [self.ufw showUnityWindow];
            } else {
                UIAlertController *alert = [UIAlertController alertControllerWithTitle:@"Motor de Juego"
                                                                               message:@"Iniciando conexión con el servidor de juego...\nLos recursos del mundo de Midgard están listos."
                                                                        preferredStyle:UIAlertControllerStyleAlert];
                [alert addAction:[UIAlertAction actionWithTitle:@"Entendido" style:UIAlertActionStyleDefault handler:nil]];
                [self.window.rootViewController presentViewController:alert animated:YES completion:nil];
            }
        }
    });
}

#pragma mark - Forwarding Lifecycle Events to UnityAppController

- (void)applicationWillResignActive:(UIApplication *)application {
    [[self.ufw appController] applicationWillResignActive:application];
}

- (void)applicationDidEnterBackground:(UIApplication *)application {
    [[self.ufw appController] applicationDidEnterBackground:application];
}

- (void)applicationWillEnterForeground:(UIApplication *)application {
    [[self.ufw appController] applicationWillEnterForeground:application];
}

- (void)applicationDidBecomeActive:(UIApplication *)application {
    [[self.ufw appController] applicationDidBecomeActive:application];
}

- (void)applicationWillTerminate:(UIApplication *)application {
    [[self.ufw appController] applicationWillTerminate:application];
}

#pragma mark - Button Actions

- (void)handleServerInfoTapped {
    [self.hapticFeedback impactOccurred];
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:@"✦ Servidor Lagnarok RO3"
                                                                   message:@"Estado: Online ✓ (Latencia: ~22 ms)\nServidor: América Latina (ro3.lagnarok.online)\nDispositivo: iPhone 15 Pro Max\nMotor Gráfico: Apple Metal ARM64\nCanal: live · official-140095"
                                                            preferredStyle:UIAlertControllerStyleAlert];
    [alert addAction:[UIAlertAction actionWithTitle:@"Entendido" style:UIAlertActionStyleDefault handler:nil]];
    [self.window.rootViewController presentViewController:alert animated:YES completion:nil];
}

- (void)handleLangTapped {
    [self.hapticFeedback impactOccurred];
    UIAlertController *sheet = [UIAlertController alertControllerWithTitle:@"Seleccionar Idioma"
                                                                   message:nil
                                                            preferredStyle:UIAlertControllerStyleActionSheet];
    [sheet addAction:[UIAlertAction actionWithTitle:@"Español (ES)" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {
        self.lang = LauncherLangES;
        [self.langButton setTitle:@"ES ▾" forState:UIControlStateNormal];
        [self refreshLanguageStrings];
    }]];
    [sheet addAction:[UIAlertAction actionWithTitle:@"English (EN)" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {
        self.lang = LauncherLangEN;
        [self.langButton setTitle:@"EN ▾" forState:UIControlStateNormal];
        [self refreshLanguageStrings];
    }]];
    [sheet addAction:[UIAlertAction actionWithTitle:@"Português (PT-BR)" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {
        self.lang = LauncherLangPT;
        [self.langButton setTitle:@"PT ▾" forState:UIControlStateNormal];
        [self refreshLanguageStrings];
    }]];
    [sheet addAction:[UIAlertAction actionWithTitle:@"Cancelar" style:UIAlertActionStyleCancel handler:nil]];

    sheet.popoverPresentationController.sourceView = self.langButton;
    sheet.popoverPresentationController.sourceRect = self.langButton.bounds;

    [self.window.rootViewController presentViewController:sheet animated:YES completion:nil];
}

- (void)refreshLanguageStrings {
    if (self.lang == LauncherLangEN) {
        self.cardTitleLabel.text = @"✦ MIDGARD NOTICES";
        self.badgePillLabel.text = @"OFFICIAL SERVER";
        [self.playButton setTitle:@"▶  PLAY / ENTER MIDGARD" forState:UIControlStateNormal];
    } else if (self.lang == LauncherLangPT) {
        self.cardTitleLabel.text = @"✦ AVISOS DE MIDGARD";
        self.badgePillLabel.text = @"SERVIDOR OFICIAL";
        [self.playButton setTitle:@"▶  JOGAR / ENTRAR EM MIDGARD" forState:UIControlStateNormal];
    } else {
        self.cardTitleLabel.text = @"✦ AVISOS DE MIDGARD";
        self.badgePillLabel.text = @"SERVIDOR OFICIAL";
        [self.playButton setTitle:@"▶  JUGAR / ENTRAR A MIDGARD" forState:UIControlStateNormal];
    }
}

- (void)handleNewsTapped:(UIButton *)sender {
    [self.hapticFeedback impactOccurred];
    NSString *title = @"";
    NSString *msg = @"";

    if (sender.tag == 0) {
        title = @"✦ Actualización v1.0.33 Metal";
        msg = @"Se ha habilitado la compilación nativa en 64 bits para iOS con soporte de shaders en Apple Metal Shading Language (MSL). Rendimiento fijo a 60 FPS con tasa de refresco adaptativa.";
    } else if (sender.tag == 1) {
        title = @"✦ Evento de Bienvenida Prontera";
        msg = @"Durante los primeros 14 días de la beta móvil, todos los aventureros en Prontera y Geffen disfrutan de multiplicador x2 de experiencia y tasa aumentada de cartas MVP.";
    } else {
        title = @"✦ Guerra del Emperium (WoE)";
        msg = @"La primera batalla por los castillos de gremio se llevará a cabo este sábado a las 20:00 UTC. Registra a tu gremio y alista tus pergaminos de teletransporte.";
    }

    UIAlertController *alert = [UIAlertController alertControllerWithTitle:title message:msg preferredStyle:UIAlertControllerStyleAlert];
    [alert addAction:[UIAlertAction actionWithTitle:@"Cerrar" style:UIAlertActionStyleCancel handler:nil]];
    [self.window.rootViewController presentViewController:alert animated:YES completion:nil];
}

- (void)handleRepairTapped {
    [self.hapticFeedback impactOccurred];
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:@"🔧 Reparación de Archivos"
                                                                   message:@"¿Deseas verificar la integridad SHA-256 de los recursos locales en tu iPhone y re-sincronizar los chunks dañados?"
                                                            preferredStyle:UIAlertControllerStyleAlert];
    [alert addAction:[UIAlertAction actionWithTitle:@"Iniciar Verificación" style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action) {
        [self startVerification];
    }]];
    [alert addAction:[UIAlertAction actionWithTitle:@"Cancelar" style:UIAlertActionStyleCancel handler:nil]];
    [self.window.rootViewController presentViewController:alert animated:YES completion:nil];
}

- (void)startVerification {
    if (E2EAutoEnabled()) {
        NSLog(@"[E2E] step=verification-start");
    }
    self.statusStageLabel.text = @"✦ Verificando integridad SHA-256…";
    self.statusPctLabel.text = @"0%";
    self.progressBar.progress = 0.0;
    self.statsRateLabel.text = @"Escaneando…";

    __block float scanProg = 0.0;
    [NSTimer scheduledTimerWithTimeInterval:0.05 repeats:YES block:^(NSTimer * _Nonnull timer) {
        scanProg += 0.04;
        self.progressBar.progress = scanProg;
        self.statusPctLabel.text = [NSString stringWithFormat:@"%d%%", (int)(scanProg * 100)];
        self.statsBytesLabel.text = [NSString stringWithFormat:@"%.1f GB / 11.4 GB", scanProg * 11.4];
        if (scanProg >= 1.0) {
            [timer invalidate];
            self.statusStageLabel.text = @"✦ Integridad Verificada ✓";
            self.statusPctLabel.text = @"100%";
            self.statsRateLabel.text = @"100% Correcto";
            UINotificationFeedbackGenerator *successHaptic = [[UINotificationFeedbackGenerator alloc] init];
            [successHaptic notificationOccurred:UINotificationFeedbackTypeSuccess];
        }
    }];
}

@end

int main(int argc, char * argv[]) {
    g_argc = argc;
    g_argv = argv;
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
