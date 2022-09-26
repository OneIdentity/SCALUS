if(test-path dist)
{
    rm -force -recurse dist
}
ng build --configuration production

if(test-path dist/app)
{
    "Copying build artifacts to ../Web for inclusion"
    foreach($item in (("main","js"), 
                      ("polyfills","js"), 
                      ("runtime", "js"), 
                      ("styles", "css")))
    {
        $path = resolve-path "dist/app/$($item[0]).*"
        $target = "../Web/$($item[0]).$($item[1])"
        "Copying $($path) => $($target)"
        Copy-Item $path $target
    }
}
else 
{
    "looks like the build failed"
}
