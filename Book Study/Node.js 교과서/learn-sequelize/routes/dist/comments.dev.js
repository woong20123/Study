"use strict";

var express = require('express');

var _require = require('../models'),
    User = _require.User,
    Comment = _require.Comment;

var router = express.Router();
router.route('/').get(function _callee(req, res, next) {
  var comments;
  return regeneratorRuntime.async(function _callee$(_context) {
    while (1) {
      switch (_context.prev = _context.next) {
        case 0:
          _context.prev = 0;
          _context.next = 3;
          return regeneratorRuntime.awrap(Comment.findAll({
            include: {
              model: User
            }
          }));

        case 3:
          comments = _context.sent;
          console.log(comments);
          res.json(comments);
          _context.next = 12;
          break;

        case 8:
          _context.prev = 8;
          _context.t0 = _context["catch"](0);
          console.error(_context.t0);
          next(_context.t0);

        case 12:
        case "end":
          return _context.stop();
      }
    }
  }, null, null, [[0, 8]]);
}).post(function _callee2(req, res, next) {
  var comment;
  return regeneratorRuntime.async(function _callee2$(_context2) {
    while (1) {
      switch (_context2.prev = _context2.next) {
        case 0:
          _context2.prev = 0;
          _context2.next = 3;
          return regeneratorRuntime.awrap(Comment.create({
            commenter: req.body.id,
            comment: req.body.comment
          }));

        case 3:
          comment = _context2.sent;
          console.log(comment);
          res.status(201).json(comment);
          _context2.next = 12;
          break;

        case 8:
          _context2.prev = 8;
          _context2.t0 = _context2["catch"](0);
          console.error(_context2.t0);
          next(_context2.t0);

        case 12:
        case "end":
          return _context2.stop();
      }
    }
  }, null, null, [[0, 8]]);
});
router.route('/:id').patch(function _callee3(req, res, next) {
  var result;
  return regeneratorRuntime.async(function _callee3$(_context3) {
    while (1) {
      switch (_context3.prev = _context3.next) {
        case 0:
          _context3.prev = 0;
          _context3.next = 3;
          return regeneratorRuntime.awrap(Comment.update({
            comment: req.body.comment
          }, {
            where: {
              id: req.params.id
            }
          }));

        case 3:
          result = _context3.sent;
          res.json(result);
          _context3.next = 11;
          break;

        case 7:
          _context3.prev = 7;
          _context3.t0 = _context3["catch"](0);
          console.error(_context3.t0);
          next(_context3.t0);

        case 11:
        case "end":
          return _context3.stop();
      }
    }
  }, null, null, [[0, 7]]);
})["delete"](function _callee4(req, res, next) {
  var result;
  return regeneratorRuntime.async(function _callee4$(_context4) {
    while (1) {
      switch (_context4.prev = _context4.next) {
        case 0:
          _context4.prev = 0;
          _context4.next = 3;
          return regeneratorRuntime.awrap(Comment.destroy({
            where: {
              id: req.params.id
            }
          }));

        case 3:
          result = _context4.sent;
          res.json(result);
          _context4.next = 11;
          break;

        case 7:
          _context4.prev = 7;
          _context4.t0 = _context4["catch"](0);
          console.error(_context4.t0);
          next(_context4.t0);

        case 11:
        case "end":
          return _context4.stop();
      }
    }
  }, null, null, [[0, 7]]);
});
module.exports = router;