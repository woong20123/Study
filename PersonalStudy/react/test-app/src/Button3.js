import React from 'react';
import style from './Button3.module.scss';
import cn from 'classnames'

function Button({ size }){
    if(size === 'big'){
        return <button className={cn(style.Button, style.big)}>큰 상자</button>
    } else{
        return <button className={cn(style.Button, style.small)}>작은 상자</button>
    }
}
export default Button;