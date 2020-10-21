if(test-path dist)
{
    rm -force -recurse dist
}
ng build --prod

if(test-path dist/app)
{
    Copy-Item dist/app/main.* ../Web/main.js
    Copy-Item dist/app/polyfills.* ../Web/polyfills.js
    Copy-Item dist/app/runtime.* ../Web/runtime.js
    Copy-Item dist/app/styles.* ../Web/styles.css
}
else 
{
    "looks like the build failed"
}
