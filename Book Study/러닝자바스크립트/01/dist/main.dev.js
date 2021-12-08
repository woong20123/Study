"use strict";

var i;
setTimeout(function () {
  console.log("hello");
}, 1500);

for (i = 5; i >= 0; i++) {
  setTimeout(function () {
    console.log(i === 0 ? "go" : i);
  }, (5 - i) * 1000);
}