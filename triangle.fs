#ifdef GL_ES
precision mediump float;
#endif
#ifdef GL_OES_standard_derivatives
  #extension GL_EXT_shader_texture_lod : enable
  #extension GL_OES_standard_derivatives : enable
#endif

const int MAX_MARCHING_STEPS = 255;
const float MIN_DIST = 0.0;
const float MAX_DIST = 1500.0;
const float EPSILON = 0.0001;

// Passed in from the vertex shader.
varying vec2 v_Texcoord;
// The texture.
uniform sampler2D u_Texture;
uniform vec2 u_resolution;
uniform vec2 u_mouse;
uniform float u_time;
uniform vec3 eye;
uniform vec3 at;

const float PI = 3.14159;

struct light {
    vec3 pos;
    vec3 ambientColor;
    vec3 diffuseColor;
    vec3 specularColor;
    float intensity;
} ;

struct rayCast {
    vec3 direction;
    float magnitude;
    int hit;
    vec3 hitPoint;
} ray;

struct primitive {
    vec3 color;
    vec3 pos;
    float sdf;
};

//global background color declaration
vec3 background = vec3(0.75,0.8,0.95);

vec4 xyzToQuat(vec3 a, float theta){
  vec4 quaternion;
  float angle = theta/2.0;
  angle = angle * PI / 180.0;
  quaternion.x = a.x * sin(angle);
  quaternion.y = a.y * sin(angle);
  quaternion.z = a.z * sin(angle);
  quaternion.w = cos(angle);
  return quaternion;
}

//order matters!!
vec4 quaternionMult(vec4 quat1, vec4 quat2){
  vec4 result;
  result.x = (quat1.w * quat2.x) + (quat1.x * quat2.w) + (quat1.y * quat2.z) - (quat1.z * quat2.y);
  result.y = (quat1.w * quat2.y) - (quat1.x * quat2.z) + (quat1.y * quat2.w) + (quat1.z * quat2.x);
  result.z = (quat1.w * quat2.z) + (quat1.x * quat2.y) - (quat1.y * quat2.x) + (quat1.z * quat2.w);
  result.w = (quat1.w * quat2.w) - (quat1.x * quat2.x) - (quat1.y * quat2.y) - (quat1.z * quat2.z);
  return result;
}

vec3 quaternionRotate(vec3 pt, vec3 a, float theta){
  vec4 rotationQuat = xyzToQuat(a,theta);
  vec4 inverse = vec4(-1.0*rotationQuat.x,-1.0*rotationQuat.y,-1.0*rotationQuat.z,rotationQuat.w);
  vec4 qpt = vec4(pt.x,pt.y,pt.z,0.0);

  vec4 qp = quaternionMult(rotationQuat,qpt);
  vec4 pPrime = quaternionMult(qp,inverse);

  return vec3(pPrime.x,pPrime.y,pPrime.z);
}


/*
    primitive for creating a box. The dimensions are reflected from the 1st
    quadrant to create the box.
*/
float sdBox( vec3 p, vec3 b )
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,vec3(0.0)));
}

/*
    primite for creating a torus. The first parameter is the position and the
    2nd controls the size of the torus and the inner circle.
*/
float sdTorus( vec3 p, vec2 t )
{
  vec2 q = vec2(length(p.xz)-t.x,p.y);
  return length(q)-t.y;
}

/*
    function for unioning two objects in the scene.
*/
float un(float d1, float d2) {
    return min(d1,d2);
}

float sceneSDF(vec3 pt) {
    float floor = sdBox(pt-vec3(0,-2.0,0), vec3(2.0,0.0,2.0));
    //float torus = sdTorus(pt,vec2(3.0,0.5));
    float torus = sdTorus(quaternionRotate(pt,vec3(1,0,0),u_time),vec2(3.0,0.5));


    float value = un(floor, torus);

    /*if() {
        ray.hit = 0; //checkerboard
    } else */if (value == torus) {
        ray.hit = 1; //red surface
    }/* else if () {
        ray.hit = 2; //mirror
    } */else if (value == floor) {
        ray.hit = 3; //textured
    }

    return value;
  }

/*
    key function of the raymarch
    find the shortest distance to the surface of an object in the scene
    eye - the origin of the ray
    direction - the normalized direction to move (direction ray is facing)
    start/end - starting point on the ray, and max distance to move
*/

float shortestDistanceToSurface(vec3 startPoint, vec3 direction, float start, float end) {
    float depth = start;
    float dist;
    for(int i = 0; i < MAX_MARCHING_STEPS; i++) {
        vec3 nextPt = startPoint + depth * direction;
        dist = sceneSDF(nextPt);
        if(dist < EPSILON)
            return depth;
        depth += dist;
        if(depth >= end) {
            return end;
        }
    }
    return end;
}


/*
    ray direction - find the normalized direction to march in from
        the eye to a single pixel on the screen.
    perameters:
    fieldOfView - vertical fov in degrees
    size - resolution of the output image
    fragCoord - the x/y coordinate of the pizel in the output
                (screen x,y not adjusted UV)
*/

vec3 rayDirection(float fieldOfView, vec2 size, vec2 fragCoord) {
    vec2 xy = fragCoord - size/2.0;
    float z = size.y/tan(radians(fieldOfView)/2.0);
    return normalize(vec3(xy,-z));
}

vec3 estimateNormal(vec3 p) {
    return normalize(vec3(
        sceneSDF(vec3(p.x + EPSILON, p.y, p.z)) - sceneSDF(vec3(p.x - EPSILON, p.y, p.z)),
        sceneSDF(vec3(p.x, p.y + EPSILON, p.z)) - sceneSDF(vec3(p.x, p.y - EPSILON, p.z)),
        sceneSDF(vec3(p.x, p.y, p.z  + EPSILON)) - sceneSDF(vec3(p.x, p.y, p.z - EPSILON))
    ));
}
//Calculates the diffused portion of the lighting
vec3 diffuseLighting(vec3 pt, light currentLight, vec3 normal) {
    vec3 lightDir = normalize(currentLight.pos-pt);
    float lDotn = dot(lightDir, normal);
    return (currentLight.diffuseColor * max(lDotn,0.0) * currentLight.intensity);
}
//Calculates the specular portion of the lighting
vec3 specularLighting(vec3 pt, vec3 normal, vec3 eye, light currentLight) {
    float shinyness = 8.0;
    vec3 l = normalize(currentLight.pos - pt);
    vec3 r = normalize(reflect(-l, normal));
    vec3 v = normalize(eye - pt);
    float rdotV = max(dot(r,v), 0.0);
    return (currentLight.specularColor * currentLight.intensity * pow(rdotV, shinyness));
}

float filterWidth(vec2 uv) {
  vec2 fw = max(abs(dFdx(uv)),abs(dFdy(uv)));
  return max(fw.x, fw.y);
}
//Creates the checkerboard pattern for the box acting as the floor
vec3 floorCheckerboard(vec3 pt) {
    vec3 color = vec3(0.981,0.985,0.995);

    float tile = floor(pt.x-(2.0 *floor(pt.x/2.0))) - floor(pt.z-(2.0*floor(pt.z/2.0)));
    if (tile < 0.0) {
        tile = 1.0;
    }
    return color * (1.0 - tile);
}
//Alternate way of creating the checkerboard pattern for the floor
vec3 floorCheckerboard2(vec2 uv){
  float width = filterWidth(uv);
  vec2 p0 = uv - 0.5 * width;
  vec2 p1 = uv + 0.5 * width;
  #define BUMPINT(x) (floor((x)/2.0) + 2.0 * max(((x)/2.0) - floor((x)/2.0) - 0.5, 0.0))
  vec2 i = (BUMPINT(p1) - BUMPINT(p0)) / width;
  float p = i.x * i.y + (1.0 - i.x) * (1.0 - i.y);
  return vec3(0.1+ 0.9 * p);
}
//Marches from the light to objects to see what parts of the scene should be in
//a shadow.
bool shadow(vec3 pt, light currentLight) {

    float dist = shortestDistanceToSurface(pt, normalize(currentLight.pos - pt), MIN_DIST+ 0.01, length(currentLight.pos - pt));

    bool hit;
    if (dist < length(currentLight.pos - pt)) {
        hit = true;
    } else if (dist == abs(length(currentLight.pos - pt))) {
        hit = false;
    }

    return  hit;
}
//Calls the other lighting functions to put together the full lighting display
vec3 lighting(vec3 pt, vec3 eye, light currentLight, vec3 objectColor) {
    vec3 ambientLight = currentLight.ambientColor;
    vec3 normal = estimateNormal(pt);

    vec3 diffuse = diffuseLighting(pt,currentLight, normal);
    vec3 specular = specularLighting(pt, normal, eye, currentLight);

    bool shadow = shadow(pt, currentLight);

    vec3 ptColor = (ambientLight + diffuse + specular) * objectColor;


    if (shadow) {
        ptColor = (float(shadow) * vec3(-0.1, -0.1, -0.1) + ambientLight) * objectColor;
    }

    return ptColor;
}
//Checks a color at a specific pixel
vec3 getObjectColor(vec3 pt) {
    vec3 sphereColor = vec3(0.830,0.164,0.276);
    vec3 floorColor = floorCheckerboard2(vec2(pt.x,pt.z));
    vec3 objectColor;
    if (ray.hit == 0) {
        objectColor = floorColor;
    } else if (ray.hit == 1) {
        objectColor = sphereColor;
    } else {
        objectColor = vec3(0,0,0);
    }

    return objectColor;
}
//Creates a mirror effect by checking the color of the point reflecting from
//one pixel out to another
vec3 mirror(vec3 pt, vec3 eye, light currentLight) {
    vec3 objectColor, normal, v, reflectedV, ogpt;
    float dist, noHit;
    vec3 mirrorColor = vec3(1.0, 1.0, 1.0);
    ogpt = pt;

    const int numBounces = 4;
    int bounces = 0;
    for (int i = 0; i < numBounces; i ++) {
        if(ray.hit == 0 || ray.hit == 1) {
            objectColor = lighting(pt, reflectedV, currentLight, getObjectColor(pt));
            break;
        }
        normal = estimateNormal(pt);
        v = eye - pt;
        reflectedV = normalize(reflect(-v, normal));
        dist = shortestDistanceToSurface(pt, reflectedV, MIN_DIST + 0.001, MAX_DIST);
        noHit = step(dist, MAX_DIST - EPSILON);

        pt += (normalize(reflectedV) * dist);
    }
    if (bounces == numBounces-1) {
        objectColor = lighting(pt, reflectedV, currentLight, vec3(0.6));
    }

    vec3 reflectionColor = ((1.0-noHit) * (background)) + (noHit * (objectColor));

    return lighting(ogpt, eye, currentLight, reflectionColor);
}
vec3 texture(vec3 pt, vec3 eye, light currentLight) {
    vec4 texcolor = texture2D(u_Texture, vec2((pt.x+2.0)/4.0,(pt.z+2.0)/4.0));
    return vec3(texcolor.x,texcolor.y,texcolor.z);
    //return vec3(0.4);
}

//Sets what type of lighting an object/pixel should have
vec3 lightingStyle(vec3 pt, vec3 eye, light currentLight) {
    vec3 color;
    vec3 objectColor = getObjectColor(pt);


    if (ray.hit == 0 || ray.hit == 1) {
        color = lighting(pt, eye, currentLight, objectColor);
    } else if (ray.hit == 2) {
        color = mirror(pt, eye, currentLight);
    } else if (ray.hit == 3) {
        color = texture(pt, eye, currentLight);
    }

    return color;
}


//lighting end

//Sets up the camera
mat3 setCamera(vec3 eye, vec3 center, float rotation) {
    vec3 forward = normalize(center - eye);
    vec3 orientation = vec3(sin(rotation),cos(rotation), 0.0);
    vec3 left = normalize(cross(forward,orientation));
    vec3 up = normalize(cross(left, forward));
    return mat3(left,up,forward);
}



//main begin
void main() {

    vec2 uv = gl_FragCoord.xy/u_resolution.xy;
    uv = 2.0 * uv - 1.0;

    mat3 toWorld = setCamera(eye, at, 0.0);
    ray.direction = toWorld * normalize(vec3(uv,2.0));

    ray.magnitude = shortestDistanceToSurface(eye, ray.direction,  MIN_DIST, MAX_DIST);

    float noHit = step(ray.magnitude, MAX_DIST - EPSILON);

    vec3 pt = eye + ray.magnitude * ray.direction;


    //start lighting
    light mainlight;
    mainlight.pos = vec3(0,6.5,0);
    mainlight.specularColor = vec3(0.9,0.9,0.9);
    mainlight.diffuseColor = vec3(0.7);
    mainlight.ambientColor = vec3(0.5);
    mainlight.intensity = 0.8;

    vec3 lightingColor = lightingStyle(pt, eye, mainlight);

    //the next line finds a vector between the light and the point on the
    //sphere
    //the next line finds the relationship between the normal of the point
    //and the light direction
    //max keeps the value zero if it's negative
    vec3 color = (1.0 - noHit)*background + (noHit*lightingColor);

    //cell shading
    //color = floor(color * 8.0)/8.0;
    #ifdef GL_OES_standard_derivatives
      gl_FragColor = vec4(color,1.0);
    #else
      gl_FragColor = vec4(0.0,0.0,0.0,1.0);
    #endif
}
