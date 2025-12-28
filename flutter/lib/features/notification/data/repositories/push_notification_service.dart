import 'dart:io';

import 'package:dio/dio.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../../../../core/network/dio_client.dart';

/// Push Notifications (FCM) with Local Notification Display
///
/// Android setup:
/// - android/app/src/main/AndroidManifest.xml
///   <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
///   <meta-data android:name="com.google.firebase.messaging.default_notification_channel_id"
///              android:value="default_channel" />
///
/// iOS setup:
/// - ios/Runner/Info.plist (optional, recommended description)
///   <key>NSUserNotificationUsageDescription</key>
///   <string>We use notifications to keep you updated.</string>
/// - Xcode -> Signing & Capabilities:
///   1) Push Notifications
///   2) Background Modes -> Remote notifications
///
/// Usage:
/// 1) In main.dart after Firebase.initializeApp():
///    await PushNotificationService.instance.init(dioClient: sl<DioClient>());
///
/// 2) After login success:
///    await PushNotificationService.instance.registerDevice();
///
/// 3) On logout:
///    await PushNotificationService.instance.unregisterDevice();
class PushNotificationService {
  PushNotificationService._();
  static final PushNotificationService instance = PushNotificationService._();

  final FirebaseMessaging _fcm = FirebaseMessaging.instance;
  late final DioClient _client;
  
  // Flutter Local Notifications
  final FlutterLocalNotificationsPlugin _localNotifications = 
      FlutterLocalNotificationsPlugin();
  
  // Android notification channel - MUST match backend's channel_id
  static const AndroidNotificationChannel _androidChannel = AndroidNotificationChannel(
    'default_channel',  // Must match backend's channel_id
    'Default Notifications',
    description: 'This channel is used for leave request notifications',
    importance: Importance.high,
    playSound: true,
  );

  bool _initialized = false;
  
  /// Callback for when notification is tapped (set from outside)
  void Function(Map<String, dynamic> data)? onNotificationTap;
  
  /// Callback for when a message is received (use to refresh notification count)
  void Function()? onMessageReceived;

  /// Call once (prefer in main after Firebase.initializeApp()).
  Future<void> init({required DioClient dioClient}) async {
    if (_initialized) return;
    _initialized = true;

    _client = dioClient;
    
    // Register background message handler
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

    // Request permission
    final granted = await _requestPermission();
    if (!granted) {
      if (kDebugMode) print('[FCM] Notifications not authorized');
      return;
    }

    // Initialize local notifications
    await _initLocalNotifications();

    // iOS foreground presentation
    if (Platform.isIOS) {
      await _fcm.setForegroundNotificationPresentationOptions(
        alert: true,
        badge: true,
        sound: true,
      );
    }

    // Foreground messages - MUST manually show notification
    FirebaseMessaging.onMessage.listen(_handleForegroundMessage);

    // User tapped notification (app was in background)
    FirebaseMessaging.onMessageOpenedApp.listen(_handleNotificationTap);
    
    // Check if app was opened from terminated state by notification
    final initialMessage = await _fcm.getInitialMessage();
    if (initialMessage != null) {
      _handleNotificationTap(initialMessage);
    }

    // Token refresh
    _fcm.onTokenRefresh.listen((newToken) async {
      if (kDebugMode) print('[FCM] Token refreshed');
      await registerDevice(forceToken: newToken);
    });
    
    if (kDebugMode) {
      final token = await _fcm.getToken();
      print('[FCM] Initialized. Token: ${token?.substring(0, 20)}...');
    }
  }
  
  /// Initialize flutter_local_notifications
  Future<void> _initLocalNotifications() async {
    // Android settings
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    
    // iOS settings
    const iosSettings = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );
    
    const initSettings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );
    
    await _localNotifications.initialize(
      initSettings,
      onDidReceiveNotificationResponse: (NotificationResponse response) {
        // Handle notification tap
        if (kDebugMode) print('[LocalNotif] Tapped: ${response.payload}');
        // You can parse the payload and navigate here
      },
    );
    
    // Create the Android notification channel
    await _localNotifications
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(_androidChannel);
    
    if (kDebugMode) print('[LocalNotif] Initialized with channel: ${_androidChannel.id}');
  }
  
  /// Handle foreground FCM messages - show local notification
  void _handleForegroundMessage(RemoteMessage message) {
    if (kDebugMode) {
      print('[FCM] Foreground message received:');
      print('  Title: ${message.notification?.title}');
      print('  Body: ${message.notification?.body}');
      print('  Data: ${message.data}');
    }
    
    final notification = message.notification;
    final android = message.notification?.android;
    
    // Show local notification for foreground messages
    if (notification != null) {
      _localNotifications.show(
        notification.hashCode,
        notification.title,
        notification.body,
        NotificationDetails(
          android: AndroidNotificationDetails(
            _androidChannel.id,
            _androidChannel.name,
            channelDescription: _androidChannel.description,
            importance: Importance.high,
            priority: Priority.high,
            icon: android?.smallIcon ?? '@mipmap/ic_launcher',
          ),
          iOS: const DarwinNotificationDetails(
            presentAlert: true,
            presentBadge: true,
            presentSound: true,
          ),
        ),
        payload: message.data.toString(),
      );
    }
    
    // Trigger callback to refresh notification count
    onMessageReceived?.call();
  }
  
  /// Handle notification tap (when app opens from notification)
  void _handleNotificationTap(RemoteMessage message) {
    if (kDebugMode) {
      print('[FCM] Notification opened: ${message.data}');
    }
    
    // Call the callback if set
    onNotificationTap?.call(message.data);
  }

  /// Returns true if authorized (authorized or provisional).
  Future<bool> _requestPermission() async {
    final settings = await _fcm.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    if (kDebugMode) {
      print('[FCM] Permission: ${settings.authorizationStatus}');
    }

    return settings.authorizationStatus == AuthorizationStatus.authorized ||
        settings.authorizationStatus == AuthorizationStatus.provisional;
  }

  /// Call after login (needs auth interceptor/token ready).
  Future<void> registerDevice({String? forceToken}) async {
    try {
      final token = forceToken ?? await _fcm.getToken();
      if (token == null) return;

      await _client.post(
        '/v1/users/devices',
        data: {
          'token': token,
          'platform': Platform.isAndroid ? 'Android' : 'iOS',
        },
      );

      if (kDebugMode) print('[FCM] Device registered');
    } on DioException catch (e) {
      if (kDebugMode) {
        print('[FCM] Register device failed: ${e.response?.data ?? e.message}');
      }
    } catch (e) {
      if (kDebugMode) print('[FCM] Register device failed: $e');
    }
  }

  /// Call on logout (best-effort).
  Future<void> unregisterDevice() async {
    try {
      final token = await _fcm.getToken();
      if (token == null) return;

      await _client.delete('/v1/users/devices/${Uri.encodeComponent(token)}');

      if (kDebugMode) print('[FCM] Device unregistered');
    } on DioException catch (e) {
      if (kDebugMode) {
        print('[FCM] Unregister device failed: ${e.response?.data ?? e.message}');
      }
    } catch (e) {
      if (kDebugMode) print('[FCM] Unregister device failed: $e');
    }
  }

  Future<String?> getFcmToken() => _fcm.getToken();
}

/// Background message handler - must be top-level function
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Note: When app is in background/terminated, FCM automatically shows
  // the notification using the 'notification' payload. No need to show manually here.
  if (kDebugMode) {
    print('[FCM] Background message: ${message.notification?.title}');
  }
}
