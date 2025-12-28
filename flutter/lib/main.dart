import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/material.dart';
import 'core/app/app.dart';
import 'core/network/dio_client.dart';
import 'core/servicelocator/servicelocator.dart';
import 'features/home/presentation/cubit/home_cubit.dart';
import 'features/notification/data/repositories/push_notification_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // MUST initialize Firebase before accessing any Firebase services
  await Firebase.initializeApp();

  // Initialize Dependency Injection (after Firebase)
  await initDependencies();
  
  final dioClient = sl<DioClient>();

  // Initialize push notifications
  await PushNotificationService.instance.init(dioClient: dioClient);
  
  // Wire up FCM -> Notification Count Refresh (after push service is initialized)
  PushNotificationService.instance.onMessageReceived = () {
    if (sl.isRegistered<HomeCubit>()) {
      sl<HomeCubit>().refreshUnreadCount();
    }
  };
  
  runApp(const LeaveApp());
}