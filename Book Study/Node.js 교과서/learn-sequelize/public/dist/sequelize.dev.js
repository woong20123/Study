"use strict";

document.querySelectorAll('#user-list tr').forEach(function (el) {
  el.addEventListener('click', function () {
    var id = el.querySelector('td').textContent;
    getComment(id);
  });
}); // 사용자 로딩

function getUser() {
  var res, users, tbody;
  return regeneratorRuntime.async(function getUser$(_context) {
    while (1) {
      switch (_context.prev = _context.next) {
        case 0:
          _context.prev = 0;
          _context.next = 3;
          return regeneratorRuntime.awrap(axios.get('/users'));

        case 3:
          res = _context.sent;
          users = res.data;
          console.log(users);
          tbody = document.querySelector('#user-list tbody'); // 초기화 구문

          tbody.innerHTML = '';
          users.map(function (user) {
            var row = document.createElement('tr');
            row.addEventListener('click', function () {
              getComment(user.id);
            }); // 로우 셀 추가

            var td = document.createElement('td');
            td.textContent = user.id;
            row.appendChild(td);
            td = document.createElement('td');
            td.textContent = user.name;
            row.appendChild(td);
            td = document.createElement('td');
            td.textContent = user.age;
            row.appendChild(td);
            td = document.createElement('td');
            td.textContent = user.married ? '기혼' : '미혼';
            row.appendChild(td);
            tbody.appendChild(row);
          });
          _context.next = 14;
          break;

        case 11:
          _context.prev = 11;
          _context.t0 = _context["catch"](0);
          console.error(_context.t0);

        case 14:
        case "end":
          return _context.stop();
      }
    }
  }, null, null, [[0, 11]]);
}

function printComment(comments, tbody) {
  comments.map(function (comment) {
    // 로우 셀 추가 
    var row = document.createElement('tr');
    var td = document.createElement('td');
    var commentID = comment.id;
    td.textContent = comment.id;
    row.appendChild(td);
    td = document.createElement('td');
    td.textContent = comment.User.name;
    row.appendChild(td);
    td = document.createElement('td');
    td.textContent = comment.comment;
    row.appendChild(td);
    var edit = document.createElement('button');
    edit.textContent = '수정';
    edit.addEventListener('click', function _callee() {
      var newComment;
      return regeneratorRuntime.async(function _callee$(_context2) {
        while (1) {
          switch (_context2.prev = _context2.next) {
            case 0:
              newComment = prompt('바꿀 내용을 입력하세요');

              if (newComment) {
                _context2.next = 3;
                break;
              }

              return _context2.abrupt("return", alert('내용을 반드시 입력하셔야 합니다.'));

            case 3:
              _context2.prev = 3;
              _context2.next = 6;
              return regeneratorRuntime.awrap(axios.patch("/comments/".concat(commentID), {
                comment: newComment
              }));

            case 6:
              getAllComment();
              _context2.next = 12;
              break;

            case 9:
              _context2.prev = 9;
              _context2.t0 = _context2["catch"](3);
              console.error(_context2.t0);

            case 12:
            case "end":
              return _context2.stop();
          }
        }
      }, null, null, [[3, 9]]);
    });
    var remove = document.createElement('button');
    remove.textContent = '삭제';
    remove.addEventListener('click', function _callee2() {
      return regeneratorRuntime.async(function _callee2$(_context3) {
        while (1) {
          switch (_context3.prev = _context3.next) {
            case 0:
              _context3.prev = 0;
              _context3.next = 3;
              return regeneratorRuntime.awrap(axios["delete"]("/comments/".concat(commentID)));

            case 3:
              getAllComment();
              _context3.next = 9;
              break;

            case 6:
              _context3.prev = 6;
              _context3.t0 = _context3["catch"](0);
              console.error(_context3.t0);

            case 9:
            case "end":
              return _context3.stop();
          }
        }
      }, null, null, [[0, 6]]);
    });
    td = document.createElement('td');
    td.appendChild(edit);
    row.appendChild(td);
    td = document.createElement('td');
    td.appendChild(remove);
    row.appendChild(td);
    tbody.appendChild(row);
  });
}

function getAllComment() {
  var res, comments, tbody;
  return regeneratorRuntime.async(function getAllComment$(_context4) {
    while (1) {
      switch (_context4.prev = _context4.next) {
        case 0:
          _context4.prev = 0;
          _context4.next = 3;
          return regeneratorRuntime.awrap(axios.get("/comments"));

        case 3:
          res = _context4.sent;
          comments = res.data;
          tbody = document.querySelector('#comment-list tbody');
          tbody.innerHTML = '';
          printComment(comments, tbody);
          _context4.next = 13;
          break;

        case 10:
          _context4.prev = 10;
          _context4.t0 = _context4["catch"](0);
          console.log(_context4.t0);

        case 13:
        case "end":
          return _context4.stop();
      }
    }
  }, null, null, [[0, 10]]);
} // 댓글 로딩


function getComment(id) {
  var res, comments, tbody;
  return regeneratorRuntime.async(function getComment$(_context5) {
    while (1) {
      switch (_context5.prev = _context5.next) {
        case 0:
          _context5.prev = 0;
          _context5.next = 3;
          return regeneratorRuntime.awrap(axios.get("/users/".concat(id, "/comments")));

        case 3:
          res = _context5.sent;
          comments = res.data;
          tbody = document.querySelector('#comment-list tbody');
          tbody.innerHTML = '';
          printComment(comments, tbody);
          _context5.next = 13;
          break;

        case 10:
          _context5.prev = 10;
          _context5.t0 = _context5["catch"](0);
          console.log(_context5.t0);

        case 13:
        case "end":
          return _context5.stop();
      }
    }
  }, null, null, [[0, 10]]);
} // 사용자 등록시 


document.getElementById('user-form').addEventListener('submit', function _callee3(e) {
  var name, age, married;
  return regeneratorRuntime.async(function _callee3$(_context6) {
    while (1) {
      switch (_context6.prev = _context6.next) {
        case 0:
          e.preventDefault();
          name = e.target.username.value;
          age = e.target.age.value;
          married = e.target.married.checked;

          if (name) {
            _context6.next = 6;
            break;
          }

          return _context6.abrupt("return", alert('이름을 입력하세요'));

        case 6:
          if (age) {
            _context6.next = 8;
            break;
          }

          return _context6.abrupt("return", alert('나이를 입력하세요'));

        case 8:
          _context6.prev = 8;
          _context6.next = 11;
          return regeneratorRuntime.awrap(axios.post('/users', {
            name: name,
            age: age,
            married: married
          }));

        case 11:
          getUser();
          _context6.next = 17;
          break;

        case 14:
          _context6.prev = 14;
          _context6.t0 = _context6["catch"](8);
          console.error(_context6.t0);

        case 17:
          e.target.username.value = '';
          e.target.age.value = '';
          e.target.married.checked = false;

        case 20:
        case "end":
          return _context6.stop();
      }
    }
  }, null, null, [[8, 14]]);
}); // 댓글 등록시

document.getElementById('comment-form').addEventListener('submit', function _callee4(e) {
  var id, comment;
  return regeneratorRuntime.async(function _callee4$(_context7) {
    while (1) {
      switch (_context7.prev = _context7.next) {
        case 0:
          e.preventDefault();
          id = e.target.userid.value;
          comment = e.target.comment.value;

          if (id) {
            _context7.next = 5;
            break;
          }

          return _context7.abrupt("return", alert('아이디를 입력하세요'));

        case 5:
          if (comment) {
            _context7.next = 7;
            break;
          }

          return _context7.abrupt("return", alert('댓글을 입력하세요'));

        case 7:
          console.log("call comment-form id = ".concat(id, " comment = ").concat(comment));
          _context7.prev = 8;
          _context7.next = 11;
          return regeneratorRuntime.awrap(axios.post('/comments', {
            id: id,
            comment: comment
          }));

        case 11:
          getComment(id);
          _context7.next = 17;
          break;

        case 14:
          _context7.prev = 14;
          _context7.t0 = _context7["catch"](8);
          console.error(_context7.t0);

        case 17:
          e.target.userid.value = '';
          e.target.comment.value = '';

        case 19:
        case "end":
          return _context7.stop();
      }
    }
  }, null, null, [[8, 14]]);
});
getAllComment();