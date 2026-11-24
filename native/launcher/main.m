#import <UIKit/UIKit.h>

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
@property (strong, nonatomic) UILabel *serverStatusLabel;
@property (strong, nonatomic) UILabel *statusStageLabel;
@property (strong, nonatomic) UILabel *statusPctLabel;
@property (strong, nonatomic) UILabel *statsBytesLabel;
@property (strong, nonatomic) UILabel *statsRateLabel;
@property (strong, nonatomic) UIProgressView *progressBar;
@property (strong, nonatomic) UIButton *playButton;
@property (assign, nonatomic) float currentProgress;
@property (strong, nonatomic) NSTimer *progressTimer;
@end

@implementation AppDelegate

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    self.window = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
    self.window.backgroundColor = [UIColor colorWithRed:15/255.0 green:23/255.0 blue:42/255.0 alpha:1.0];

    UIViewController *vc = [[UIViewController alloc] init];
    vc.view.frame = self.window.bounds;

    CGFloat screenWidth = self.window.bounds.size.width;
    CGFloat screenHeight = self.window.bounds.size.height;

    // 1. Background Image (assets/bg.jpg)
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

    // Subtle sky gradient overlay
    CAGradientLayer *bgGradient = [CAGradientLayer layer];
    bgGradient.frame = self.window.bounds;
    bgGradient.colors = @[
        (id)[UIColor colorWithRed:215/255.0 green:233/255.0 blue:255/255.0 alpha:0.35].CGColor,
        (id)[UIColor colorWithRed:244/255.0 green:248/255.0 blue:255/255.0 alpha:0.45].CGColor,
        (id)[UIColor colorWithRed:15/255.0 green:23/255.0 blue:42/255.0 alpha:0.82].CGColor
    ];
    bgGradient.locations = @[@0.0, @0.45, @1.0];
    [vc.view.layer addSublayer:bgGradient];

    CGFloat topMargin = 56.0;
    if (@available(iOS 11.0, *)) {
        UIWindow *keyWindow = [UIApplication sharedApplication].keyWindow;
        if (keyWindow && keyWindow.safeAreaInsets.top > 0) {
            topMargin = keyWindow.safeAreaInsets.top + 6.0;
        }
    }

    // 2. Top Glass Bar
    UIVisualEffectView *topBar = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleExtraLight]];
    topBar.frame = CGRectMake(18, topMargin, screenWidth - 36, 44);
    topBar.layer.cornerRadius = 16;
    topBar.layer.masksToBounds = YES;
    topBar.layer.borderWidth = 1.0;
    topBar.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.85].CGColor;
    [vc.view addSubview:topBar];

    // Green pulse dot + Server name
    UIView *pulseDot = [[UIView alloc] initWithFrame:CGRectMake(14, 18, 8, 8)];
    pulseDot.backgroundColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0];
    pulseDot.layer.cornerRadius = 4;
    pulseDot.layer.shadowColor = [UIColor colorWithRed:16/255.0 green:185/255.0 blue:129/255.0 alpha:1.0].CGColor;
    pulseDot.layer.shadowRadius = 4;
    pulseDot.layer.shadowOpacity = 0.8;
    [topBar.contentView addSubview:pulseDot];

    self.serverStatusLabel = [[UILabel alloc] initWithFrame:CGRectMake(28, 0, 200, 44)];
    self.serverStatusLabel.text = @"Online · Midgard Oficial";
    self.serverStatusLabel.font = [UIFont systemFontOfSize:12 weight:UIFontWeightBold];
    self.serverStatusLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [topBar.contentView addSubview:self.serverStatusLabel];

    UILabel *langPill = [[UILabel alloc] initWithFrame:CGRectMake(topBar.bounds.size.width - 70, 9, 56, 26)];
    langPill.text = @"ES ▾";
    langPill.textAlignment = NSTextAlignmentCenter;
    langPill.font = [UIFont systemFontOfSize:11 weight:UIFontWeightBold];
    langPill.textColor = [UIColor colorWithRed:2/255.0 green:132/255.0 blue:199/255.0 alpha:1.0];
    langPill.backgroundColor = [UIColor colorWithRed:2/255.0 green:132/255.0 blue:199/255.0 alpha:0.12];
    langPill.layer.cornerRadius = 8;
    langPill.layer.masksToBounds = YES;
    [topBar.contentView addSubview:langPill];

    // 3. Hero Logo Area
    CGFloat logoTop = topMargin + 52.0;
    CGFloat logoHeight = 120.0;
    UIImageView *logoView = [[UIImageView alloc] initWithFrame:CGRectMake((screenWidth - 250) / 2.0, logoTop, 250, logoHeight)];
    logoView.contentMode = UIViewContentModeScaleAspectFit;

    NSString *logoPath = [[NSBundle mainBundle] pathForResource:@"logo-hero" ofType:@"png" inDirectory:@"assets"];
    if (!logoPath) logoPath = [[NSBundle mainBundle] pathForResource:@"logo-hero" ofType:@"png"];
    if (logoPath) {
        logoView.image = [UIImage imageWithContentsOfFile:logoPath];
    }
    logoView.layer.shadowColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0].CGColor;
    logoView.layer.shadowOffset = CGSizeMake(0, 6);
    logoView.layer.shadowRadius = 14;
    logoView.layer.shadowOpacity = 0.35;
    [vc.view addSubview:logoView];

    // 4. Central Notices Glass Card
    CGFloat cardTop = logoTop + logoHeight + 10.0;
    CGFloat cardHeight = 230.0;
    UIVisualEffectView *newsCard = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleExtraLight]];
    newsCard.frame = CGRectMake(18, cardTop, screenWidth - 36, cardHeight);
    newsCard.layer.cornerRadius = 24;
    newsCard.layer.masksToBounds = YES;
    newsCard.layer.borderWidth = 1.0;
    newsCard.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.9].CGColor;
    [vc.view addSubview:newsCard];

    // Card Header
    UILabel *cardTitle = [[UILabel alloc] initWithFrame:CGRectMake(18, 14, 180, 24)];
    cardTitle.text = @"✦ AVISOS DE MIDGARD";
    cardTitle.font = [UIFont systemFontOfSize:13 weight:UIFontWeightHeavy];
    cardTitle.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [newsCard.contentView addSubview:cardTitle];

    UILabel *badgePill = [[UILabel alloc] initWithFrame:CGRectMake(newsCard.bounds.size.width - 128, 14, 110, 22)];
    badgePill.text = @"SERVIDOR OFICIAL";
    badgePill.textAlignment = NSTextAlignmentCenter;
    badgePill.font = [UIFont systemFontOfSize:9 weight:UIFontWeightHeavy];
    badgePill.textColor = [UIColor colorWithRed:180/255.0 green:83/255.0 blue:9/255.0 alpha:1.0];
    badgePill.backgroundColor = [UIColor colorWithRed:254/255.0 green:243/255.0 blue:199/255.0 alpha:1.0];
    badgePill.layer.cornerRadius = 8;
    badgePill.layer.borderWidth = 1.0;
    badgePill.layer.borderColor = [UIColor colorWithRed:253/255.0 green:230/255.0 blue:138/255.0 alpha:1.0].CGColor;
    badgePill.layer.masksToBounds = YES;
    [newsCard.contentView addSubview:badgePill];

    // Gold hairline
    UIView *divider = [[UIView alloc] initWithFrame:CGRectMake(18, 44, newsCard.bounds.size.width - 36, 1)];
    divider.backgroundColor = [UIColor colorWithRed:212/255.0 green:168/255.0 blue:87/255.0 alpha:0.35];
    [newsCard.contentView addSubview:divider];

    // Notice items
    NSArray *items = @[
        @{@"tag": @"UPDATE", @"color": [UIColor colorWithRed:29/255.0 green:78/255.0 blue:216/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:219/255.0 green:234/255.0 blue:254/255.0 alpha:1.0], @"text": @"Cliente nativo iOS v1.0.33 habilitado con soporte de Metal.", @"date": @"Hoy"},
        @{@"tag": @"EVENTO", @"color": [UIColor colorWithRed:190/255.0 green:24/255.0 blue:93/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:252/255.0 green:231/255.0 blue:243/255.0 alpha:1.0], @"text": @"Bienvenida a Midgard: Multiplicador x2 EXP y Drop en Prontera.", @"date": @"22 Nov"},
        @{@"tag": @"GREMIO", @"color": [UIColor colorWithRed:67/255.0 green:56/255.0 blue:202/255.0 alpha:1.0], @"bg": [UIColor colorWithRed:224/255.0 green:231/255.0 blue:255/255.0 alpha:1.0], @"text": @"Guerra del Emperium (WoE) programada para este sábado 20:00 UTC.", @"date": @"20 Nov"}
    ];

    CGFloat itemY = 52.0;
    for (NSDictionary *info in items) {
        UILabel *tagLabel = [[UILabel alloc] initWithFrame:CGRectMake(18, itemY + 2, 48, 18)];
        tagLabel.text = info[@"tag"];
        tagLabel.textAlignment = NSTextAlignmentCenter;
        tagLabel.font = [UIFont systemFontOfSize:9 weight:UIFontWeightHeavy];
        tagLabel.textColor = info[@"color"];
        tagLabel.backgroundColor = info[@"bg"];
        tagLabel.layer.cornerRadius = 5;
        tagLabel.layer.masksToBounds = YES;
        [newsCard.contentView addSubview:tagLabel];

        UILabel *descLabel = [[UILabel alloc] initWithFrame:CGRectMake(74, itemY, newsCard.bounds.size.width - 120, 36)];
        descLabel.text = info[@"text"];
        descLabel.font = [UIFont systemFontOfSize:11.5 weight:UIFontWeightMedium];
        descLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
        descLabel.numberOfLines = 2;
        [newsCard.contentView addSubview:descLabel];

        UILabel *dateLabel = [[UILabel alloc] initWithFrame:CGRectMake(newsCard.bounds.size.width - 44, itemY + 2, 36, 16)];
        dateLabel.text = info[@"date"];
        dateLabel.textAlignment = NSTextAlignmentRight;
        dateLabel.font = [UIFont systemFontOfSize:9.5 weight:UIFontWeightRegular];
        dateLabel.textColor = [UIColor colorWithRed:124/255.0 green:143/255.0 blue:166/255.0 alpha:1.0];
        [newsCard.contentView addSubview:dateLabel];

        itemY += 54.0;
    }

    // 5. Floating Bottom Dock
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
    [vc.view addSubview:dockView];

    // Status row
    self.statusStageLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 14, 200, 22)];
    self.statusStageLabel.text = @"✦ Descargando recursos…";
    self.statusStageLabel.font = [UIFont systemFontOfSize:13 weight:UIFontWeightHeavy];
    self.statusStageLabel.textColor = [UIColor colorWithRed:30/255.0 green:45/255.0 blue:61/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statusStageLabel];

    self.statusPctLabel = [[UILabel alloc] initWithFrame:CGRectMake(dockView.bounds.size.width - 80, 12, 60, 24)];
    self.statusPctLabel.text = @"68%";
    self.statusPctLabel.textAlignment = NSTextAlignmentRight;
    self.statusPctLabel.font = [UIFont systemFontOfSize:17 weight:UIFontWeightHeavy];
    self.statusPctLabel.textColor = [UIColor colorWithRed:37/255.0 green:99/255.0 blue:235/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statusPctLabel];

    // Sleek progress bar
    self.progressBar = [[UIProgressView alloc] initWithProgressViewStyle:UIProgressViewStyleDefault];
    self.progressBar.frame = CGRectMake(20, 44, dockView.bounds.size.width - 40, 12);
    self.progressBar.layer.cornerRadius = 6;
    self.progressBar.clipsToBounds = YES;
    self.progressBar.progressTintColor = [UIColor colorWithRed:59/255.0 green:130/255.0 blue:246/255.0 alpha:1.0];
    self.progressBar.trackTintColor = [UIColor colorWithRed:200/255.0 green:215/255.0 blue:235/255.0 alpha:0.6];
    self.progressBar.progress = 0.68;
    [dockView.contentView addSubview:self.progressBar];

    // Stats row
    self.statsBytesLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 62, 140, 18)];
    self.statsBytesLabel.text = @"7.7 GB / 11.4 GB";
    self.statsBytesLabel.font = [UIFont systemFontOfSize:11 weight:UIFontWeightMedium];
    self.statsBytesLabel.textColor = [UIColor colorWithRed:90/255.0 green:113/255.0 blue:138/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statsBytesLabel];

    self.statsRateLabel = [[UILabel alloc] initWithFrame:CGRectMake(dockView.bounds.size.width - 180, 62, 160, 18)];
    self.statsRateLabel.text = @"8.4 MB/s · ETA: 7 min";
    self.statsRateLabel.textAlignment = NSTextAlignmentRight;
    self.statsRateLabel.font = [UIFont systemFontOfSize:11 weight:UIFontWeightMedium];
    self.statsRateLabel.textColor = [UIColor colorWithRed:90/255.0 green:113/255.0 blue:138/255.0 alpha:1.0];
    [dockView.contentView addSubview:self.statsRateLabel];

    // Action buttons
    UIButton *btnRepair = [UIButton buttonWithType:UIButtonTypeCustom];
    btnRepair.frame = CGRectMake(20, 88, 48, 48);
    btnRepair.backgroundColor = [UIColor colorWithWhite:1.0 alpha:0.95];
    btnRepair.layer.cornerRadius = 16;
    btnRepair.layer.borderWidth = 1.0;
    btnRepair.layer.borderColor = [UIColor colorWithRed:200/255.0 green:215/255.0 blue:235/255.0 alpha:0.8].CGColor;
    [btnRepair setTitle:@"🔧" forState:UIControlStateNormal];
    btnRepair.titleLabel.font = [UIFont systemFontOfSize:20];
    [dockView.contentView addSubview:btnRepair];

    self.playButton = [UIButton buttonWithType:UIButtonTypeCustom];
    self.playButton.frame = CGRectMake(78, 88, dockView.bounds.size.width - 98, 48);
    self.playButton.backgroundColor = [UIColor colorWithRed:37/255.0 green:99/255.0 blue:235/255.0 alpha:1.0];
    self.playButton.layer.cornerRadius = 16;
    self.playButton.layer.borderWidth = 1.0;
    self.playButton.layer.borderColor = [UIColor colorWithWhite:1.0 alpha:0.4].CGColor;
    [self.playButton setTitle:@"▶  DESCARGANDO JUEGO" forState:UIControlStateNormal];
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

    // Check online status in background
    [self checkServerStatus];

    return YES;
}

- (void)checkServerStatus {
    NSURL *url = [NSURL URLWithString:@"https://ro3.lagnarok.online/channels/live.json"];
    NSURLSessionDataTask *task = [[NSURLSession sharedSession] dataTaskWithURL:url completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (!error && data) {
                NSDictionary *json = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                NSString *ver = json[@"version"] ?: @"live";
                self.serverStatusLabel.text = [NSString stringWithFormat:@"Online · Midgard (%@)", ver];
            }
        });
    }];
    [task resume];
}

- (void)handlePlayTapped {
    NSLog(@"[Ro3Ios] Play button tapped!");
}

@end

int main(int argc, char * argv[]) {
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
