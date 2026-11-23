#import <UIKit/UIKit.h>

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
@property (strong, nonatomic) UILabel *statusLabel;
@property (strong, nonatomic) UIProgressView *progressBar;
@end

@implementation AppDelegate

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    self.window = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
    self.window.backgroundColor = [UIColor colorWithRed:16/255.0 green:20/255.0 blue:30/255.0 alpha:1.0];

    UIViewController *vc = [[UIViewController alloc] init];
    vc.view.backgroundColor = [UIColor colorWithRed:16/255.0 green:20/255.0 blue:30/255.0 alpha:1.0];

    CGFloat width = self.window.bounds.size.width;

    UILabel *titleLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 140, width - 40, 50)];
    titleLabel.text = @"Lagnarok RO3";
    titleLabel.textColor = [UIColor colorWithRed:246/255.0 green:214/255.0 blue:150/255.0 alpha:1.0];
    titleLabel.font = [UIFont boldSystemFontOfSize:34];
    titleLabel.textAlignment = NSTextAlignmentCenter;
    [vc.view addSubview:titleLabel];

    UILabel *subLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 195, width - 40, 30)];
    subLabel.text = @"Cliente iOS Nativo (Apple Metal / ARM64)";
    subLabel.textColor = [UIColor colorWithRed:160/255.0 green:170/255.0 blue:190/255.0 alpha:1.0];
    subLabel.font = [UIFont systemFontOfSize:15];
    subLabel.textAlignment = NSTextAlignmentCenter;
    [vc.view addSubview:subLabel];

    self.statusLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, 260, width - 40, 60)];
    self.statusLabel.text = @"Conectando al servidor...\nhttps://ro3.lagnarok.online";
    self.statusLabel.numberOfLines = 2;
    self.statusLabel.textColor = [UIColor whiteColor];
    self.statusLabel.font = [UIFont systemFontOfSize:17];
    self.statusLabel.textAlignment = NSTextAlignmentCenter;
    [vc.view addSubview:self.statusLabel];

    self.progressBar = [[UIProgressView alloc] initWithProgressViewStyle:UIProgressViewStyleDefault];
    self.progressBar.frame = CGRectMake(40, 340, width - 80, 20);
    self.progressBar.progressTintColor = [UIColor colorWithRed:236/255.0 green:170/255.0 blue:60/255.0 alpha:1.0];
    self.progressBar.trackTintColor = [UIColor colorWithRed:44/255.0 green:52/255.0 blue:70/255.0 alpha:1.0];
    self.progressBar.progress = 0.1;
    [vc.view addSubview:self.progressBar];

    NSURL *url = [NSURL URLWithString:@"https://ro3.lagnarok.online/channels/live.json"];
    NSURLSessionDataTask *task = [[NSURLSession sharedSession] dataTaskWithURL:url completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error) {
                self.statusLabel.text = [NSString stringWithFormat:@"Servidor no alcanzable:\n%@", error.localizedDescription];
            } else {
                NSDictionary *json = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                NSString *version = json[@"version"] ?: @"live";
                self.statusLabel.text = [NSString stringWithFormat:@"Servidor Online ✓\nCanal: live · Versión: %@", version];
                [self.progressBar setProgress:1.0 animated:YES];
            }
        });
    }];
    [task resume];

    self.window.rootViewController = vc;
    [self.window makeKeyAndVisible];
    return YES;
}

@end

int main(int argc, char * argv[]) {
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
