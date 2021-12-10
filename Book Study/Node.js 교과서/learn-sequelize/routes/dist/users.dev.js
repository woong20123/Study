"use strict";

var express = require('express');

var User = require('../models/user');

var Comment = require('../models/comment');

var router = express.Router();
router.route('/').get(function _callee(req, res, next) {
  var users;
  return regeneratorRuntime.async(function _callee$(_context) {
    while (1) {
      switch (_context.prev = _context.next) {
        case 0:
          _context.prev = 0;
          _context.next = 3;
          return regeneratorRuntime.awrap(User.findAll());

        case 3:
          users = _context.sent;
          res.json(users);
          _context.next = 11;
          break;

        case 7:
          _context.prev = 7;
          _context.t0 = _context["catch"](0);
          console.error(_context.t0);
          next(_context.t0);

        case 11:
        case "end":
          return _context.stop();
      }
    }
  }, null, null, [[0, 7]]);
}).post(function _callee2(req, res, next) {
  var user;
  return regeneratorRuntime.async(function _callee2$(_context2) {
    while (1) {
      switch (_context2.prev = _context2.next) {
        case 0:
          _context2.prev = 0;
          _context2.next = 3;
          return regeneratorRuntime.awrap(User.create({
            name: req.body.name,
            age: req.body.age,
            married: req.body.married
          }));

        case 3:
          user = _context2.sent;
          console.log(user);
          res.status(201).json(user);
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
router.get('/:id/comments', function _callee3(req, res, next) {
  var comments;
  return regeneratorRuntime.async(function _callee3$(_context3) {
    while (1) {
      switch (_context3.prev = _context3.next) {
        case 0:
          _context3.prev = 0;
          _context3.next = 3;
          return regeneratorRuntime.awrap(Comment.findAll({
            include: {
              model: User,
              where: {
                id: req.params.id
              }
            }
          }));

        case 3:
          comments = _context3.sent;
          console.log(comments);
          res.json(comments);
          _context3.next = 12;
          break;

        case 8:
          _context3.prev = 8;
          _context3.t0 = _context3["catch"](0);
          console.error(_context3.t0);
          next(_context3.t0);

        case 12:
        case "end":
          return _context3.stop();
      }
    }
  }, null, null, [[0, 8]]);
});
module.exports = router;