import React from 'react';
import style from './Box3.module.scss';
import cn from 'classnames'

function Box({ size }){
    if(size === 'big'){
        return <div className={cn(style.Button, style.big)}>큰 상자</div>
    } else{
        return <div className={cn(style.Button, style.small)}>작은 상자</div>
    }
}
export default Box;