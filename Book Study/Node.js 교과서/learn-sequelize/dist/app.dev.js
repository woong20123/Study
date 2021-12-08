"use strict";

var express = require('express');

var path = require('path');

var morgan = require('morgan');

var nunjucks = require('nunjucks');

var _require = require('./models'),
    sequelize = _require.sequelize;

var indexRouter = require('./routes');

var usersRouter = require('./routes/users');

var commentsRouter = require('./routes/comments');

var app = express();
app.set('port', process.env.PORT || 3001);
app.set('view engine', 'html');
nunjucks.configure('views', {
  express: app,
  watch: true
});
sequelize.sync({
  force: false
}).then(function () {
  console.log('데이터베이스 연결 성공');
})["catch"](function (err) {
  console.error(err);
});
app.use(morgan('dev'));
app.use(express["static"](path.join(__dirname, 'public')));
app.use(express.json());
app.use(express.urlencoded({
  extended: false
}));
app.use('/', indexRouter);
app.use('/users', usersRouter);
app.use('/comments', commentsRouter);
app.use(function (req, res, next) {
  var error = new Error("".concat(req.method, " ").concat(req.url, " \uB77C\uC6B0\uD130\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4."));
  error.status = 404;
  next(error);
});
app.use(function (err, req, res, next) {
  res.locals.message = err.message;
  res.locals.error = process.env.NODE_ENV !== 'production' ? err : {};
  res.status(err.status || 500);
  res.render('error');
});
app.listen(app.get('port'), function () {
  console.log(app.get('port'), '번 포트에서 대기중');
});