import 'package:intl/intl.dart';

final _currencyFormat =
    NumberFormat.currency(locale: 'vi_VN', symbol: '₫', decimalDigits: 0);
final _dateFormat = DateFormat('dd/MM/yyyy');

String formatVnd(num amount) => _currencyFormat.format(amount);

String formatPortalDate(DateTime date) => _dateFormat.format(date);
