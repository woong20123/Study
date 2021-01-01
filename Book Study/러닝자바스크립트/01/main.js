
$(document).ready(function(){
    'use strict';
    paper.install(window);
    paper.setup(document.getElementById('mainCanvas'));

    // TODO
    let c = Shape.Circle(200, 200, 80);
    c.fillColor = 'black';

    let text = new PointText(200, 200);
    text.justification = 'center';
    text.fillColor = 'white';
    text.fontSize = 20;
    text.content = 'hello world';

    let tool = new Tool();
    tool.onMouseDown = function(event){
        let c = Shape.Circle(event.point, 20);
        c.fillColor = 'green';
    }
    
    paper.view.draw();
});
