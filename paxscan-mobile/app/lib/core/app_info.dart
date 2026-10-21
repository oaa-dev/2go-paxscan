import 'dart:io';

abstract final class AppInfo {
  static const version = '0.1.0';

  static String get platform => Platform.isIOS ? 'ios' : 'android';
}
